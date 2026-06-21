namespace WatchParty.Application.Abstractions.Security;

/// <summary>
/// Deterministic hash for opaque tokens (refresh / email / reset). We store only
/// the hash and look tokens up by it, so a DB leak never exposes usable tokens.
/// </summary>
public interface ITokenHasher
{
    string Hash(string rawToken);
}
