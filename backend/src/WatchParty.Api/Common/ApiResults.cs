using Microsoft.AspNetCore.Mvc;
using WatchParty.Contracts.Common;
using WatchParty.Domain.Common;

namespace WatchParty.Api.Common;

/// <summary>
/// Maps the application's <see cref="Result"/> outcomes to HTTP responses using the
/// stable <see cref="ApiErrorResponse"/> envelope (architecture §15).
/// </summary>
public static class ApiResults
{
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : Problem(result.Error);

    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess
            ? new OkResult()
            : Problem(result.Error);

    public static IActionResult ToCreatedResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created }
            : Problem(result.Error);

    private static IActionResult Problem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        var body = new ApiErrorResponse(error.Code, error.Message);
        return new ObjectResult(body) { StatusCode = statusCode };
    }
}
