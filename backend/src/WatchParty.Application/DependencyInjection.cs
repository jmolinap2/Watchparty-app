using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Common;
using WatchParty.Application.Identity;
using WatchParty.Application.Rooms;

namespace WatchParty.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddScoped<IDispatcher, Dispatcher>();

        RegisterHandlers(services, assembly, typeof(ICommandHandler<,>));
        RegisterHandlers(services, assembly, typeof(IQueryHandler<,>));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Application services (orchestration shared across use cases).
        services.AddScoped<AuthTokenIssuer>();
        services.AddScoped<RoomDetailComposer>();

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly, Type openHandlerType)
    {
        var implementations = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false });

        foreach (var implementation in implementations)
        {
            var handlerInterfaces = implementation.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openHandlerType);

            foreach (var handlerInterface in handlerInterfaces)
            {
                services.AddScoped(handlerInterface, implementation);
            }
        }
    }
}
