using Microsoft.EntityFrameworkCore;
using WatchParty.Application.Abstractions.Persistence;
using WatchParty.Domain.Rooms;

namespace WatchParty.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository(WatchPartyDbContext dbContext) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Rooms
            .Include(room => room.Members)
            .FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public Task<Room?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var roomCode = RoomCode.FromTrusted(code);
        return dbContext.Rooms
            .Include(room => room.Members)
            .FirstOrDefaultAsync(room => room.Code == roomCode, cancellationToken);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        var roomCode = RoomCode.FromTrusted(code);
        return dbContext.Rooms.AnyAsync(room => room.Code == roomCode, cancellationToken);
    }

    public async Task AddAsync(Room room, CancellationToken cancellationToken) =>
        await dbContext.Rooms.AddAsync(room, cancellationToken);

    public void Update(Room room) => dbContext.Rooms.Update(room);
}
