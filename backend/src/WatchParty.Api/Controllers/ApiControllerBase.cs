using Microsoft.AspNetCore.Mvc;
using WatchParty.Api.Common;
using WatchParty.Application.Abstractions.Messaging;

namespace WatchParty.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase(IDispatcher dispatcher) : ControllerBase
{
    protected IDispatcher Dispatcher => dispatcher;

    protected Guid UserId => User.GetUserId();

    protected bool IsAdmin => User.IsAdmin();

    protected string? IpAddress => HttpContext.Connection.RemoteIpAddress?.ToString();
}
