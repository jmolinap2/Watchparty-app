using System.Security.Cryptography;
using System.Text;
using WatchParty.Application.Abstractions.Security;

namespace WatchParty.Infrastructure.Security;

/// <summary>
/// Deterministic SHA-256 hash (hex) for opaque tokens. Deterministic so tokens can
/// be looked up by hash; only the hash is ever stored.
/// </summary>
public sealed class Sha256TokenHasher : ITokenHasher
{
    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexStringLower(bytes);
    }
}
