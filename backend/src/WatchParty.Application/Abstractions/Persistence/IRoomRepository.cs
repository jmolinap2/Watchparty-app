using WatchParty.Domain.Rooms;

namespace WatchParty.Application.Abstractions.Persistence;

public interface IRoomRepository
{
    /// <summary>Loads the room with its members (the full aggregate) for mutation.</summary>
    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Room?> GetByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);
    Task AddAsync(Room room, CancellationToken cancellationToken);
    void Update(Room room);
}
