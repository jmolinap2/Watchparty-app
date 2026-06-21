using WatchParty.Domain.Admin;
using Xunit;

namespace WatchParty.Tests.Domain;

public sealed class AuditLogTests
{
    [Fact]
    public void HttpRequest_captures_request_execution_metadata()
    {
        var actorUserId = Guid.NewGuid();

        var log = AuditLog.HttpRequest(
            "GET /api/rooms",
            actorUserId,
            "127.0.0.1",
            "RoomsController.Get",
            "GET /api/rooms",
            "GET",
            "/api/rooms",
            200,
            42,
            "test-agent",
            "corr-123",
            exception: null);

        Assert.Equal(AuditCategory.Http, log.Category);
        Assert.Equal(actorUserId, log.ActorUserId);
        Assert.Equal("HttpRequest", log.TargetType);
        Assert.Equal("corr-123", log.TargetId);
        Assert.Equal("/api/rooms", log.RequestPath);
        Assert.Equal(200, log.StatusCode);
        Assert.Equal(42, log.DurationMs);
    }
}
