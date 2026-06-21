namespace WatchParty.Application.Abstractions.Services;

/// <summary>Generates a candidate room invite code (uniqueness is checked by the use case).</summary>
public interface IInviteCodeGenerator
{
    string Generate();
}
