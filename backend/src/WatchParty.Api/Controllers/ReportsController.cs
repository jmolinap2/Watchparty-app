using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;
using WatchParty.Application.Reports;
using WatchParty.Contracts.Reports;

namespace WatchParty.Api.Controllers;

[Authorize]
[Route("api/reports")]
public sealed class ReportsController(IDispatcher dispatcher) : ApiControllerBase(dispatcher)
{
    [HttpPost("users")]
    public async Task<IActionResult> ReportUser(ReportUserRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ReportUserCommand(UserId, request.TargetUserId, request.RoomId, request.Reason), cancellationToken)).ToCreatedResult();

    [HttpPost("messages")]
    public async Task<IActionResult> ReportMessage(ReportMessageRequest request, CancellationToken cancellationToken) =>
        (await Dispatcher.Send(new ReportMessageCommand(UserId, request.MessageId, request.Reason), cancellationToken)).ToCreatedResult();

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        (await Dispatcher.Query(new GetMyReportsQuery(UserId), cancellationToken)).ToActionResult();
}
