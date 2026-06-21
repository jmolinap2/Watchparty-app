using FluentValidation;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Contracts.Users;
using WatchParty.Domain.Common;
using WatchParty.Domain.Identity;

namespace WatchParty.Application.Users;

public sealed record UpdateProfileCommand(Guid UserId, string DisplayName, bool IsPrivate)
    : ICommand<Result<UserProfileDto>>;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(User.MaxDisplayNameLength);
    }
}

public sealed class UpdateProfileCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateProfileCommand, Result<UserProfileDto>>
{
    public async Task<Result<UserProfileDto>> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return DomainErrors.Users.NotFound;
        }

        var result = user.UpdateProfile(command.DisplayName, command.IsPrivate);
        if (result.IsFailure)
        {
            return result.Error;
        }

        userRepository.Update(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user.ToProfileDto();
    }
}
