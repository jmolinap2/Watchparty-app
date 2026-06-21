using System.Security.Cryptography;
using System.Text;
using WatchParty.Application.Abstractions.Services;
using WatchParty.Domain.Rooms;

namespace WatchParty.Infrastructure.Services;

/// <summary>Generates a random room code from the unambiguous alphabet (uniqueness checked by the use case).</summary>
public sealed class InviteCodeGenerator : IInviteCodeGenerator
{
    public string Generate()
    {
        var builder = new StringBuilder(RoomCode.Length);
        for (var i = 0; i < RoomCode.Length; i++)
        {
            var index = RandomNumberGenerator.GetInt32(RoomCode.Alphabet.Length);
            builder.Append(RoomCode.Alphabet[index]);
        }

        return builder.ToString();
    }
}
