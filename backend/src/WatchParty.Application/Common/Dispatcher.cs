using System.Collections.Concurrent;
using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using WatchParty.Application.Abstractions.Messaging;

namespace WatchParty.Application.Common;

/// <summary>
/// Resolves and invokes the handler for a command/query, running the
/// contract-level validation pipeline first (architecture §15, level "Contrato").
/// </summary>
public sealed class Dispatcher(IServiceProvider provider) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> HandleMethods = new();

    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        => Execute<TResponse>(command, typeof(ICommandHandler<,>), cancellationToken);

    public Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        => Execute<TResponse>(query, typeof(IQueryHandler<,>), cancellationToken);

    private async Task<TResponse> Execute<TResponse>(object message, Type openHandlerType, CancellationToken cancellationToken)
    {
        await ValidateAsync(message, cancellationToken);

        var handlerType = openHandlerType.MakeGenericType(message.GetType(), typeof(TResponse));
        var handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for '{message.GetType().Name}'.");

        var method = HandleMethods.GetOrAdd(handlerType, static t => t.GetMethod("Handle")!);
        return await (Task<TResponse>)method.Invoke(handler, [message, cancellationToken])!;
    }

    private async Task ValidateAsync(object message, CancellationToken cancellationToken)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(message.GetType());
        var validators = ((IEnumerable<object?>)provider.GetServices(validatorType))
            .OfType<IValidator>()
            .ToList();

        if (validators.Count == 0)
        {
            return;
        }

        var contextType = typeof(ValidationContext<>).MakeGenericType(message.GetType());
        var context = (IValidationContext)Activator.CreateInstance(contextType, message)!;

        var failures = new List<ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, cancellationToken);
            if (!result.IsValid)
            {
                failures.AddRange(result.Errors);
            }
        }

        if (failures.Count == 0)
        {
            return;
        }

        var errors = failures
            .GroupBy(f => ToCamelCase(f.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        throw new ValidationException(errors);
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }
}
