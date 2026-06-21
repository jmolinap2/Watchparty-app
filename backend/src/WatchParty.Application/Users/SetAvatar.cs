using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Users;
using WatchParty.Domain.Common;

namespace WatchParty.Application.Users;

public sealed record SetAvatarCommand(Guid UserId, string? AvatarUrl) : ICommand<Result<UserProfileDto>>;

public sealed class SetAvatarValidator : AbstractValidator<SetAvatarCommand>
{
    public SetAvatarValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl), () =>
        {
            RuleFor(x => x.AvatarUrl)
                .MaximumLength(2048)
                .Must(BeHttpsUrl).WithMessage("Avatar URL must be a valid HTTPS URL.");
        });
    }

    private static bool BeHttpsUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}

public sealed class SetAvatarCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<SetAvatarCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(SetAvatarCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Users.NotFound;
        }

        user.SetAvatar(command.AvatarUrl);
        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToProfileDto();
    }
}
