namespace WatchParty.Application.Abstractions.Security;

/// <summary>Generates cryptographically-random, URL-safe opaque tokens.</summary>
public interface ISecureTokenGenerator
{
    string Generate(int byteLength = 32);
}
