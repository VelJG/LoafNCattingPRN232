using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.WebApi.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected async Task<IActionResult> HandleAsync<T>(Func<Task<T>> action)
        => await HandleAsync(action, result => Ok(result));

    protected async Task<IActionResult> HandleAsync<T>(
        Func<Task<T>> action,
        Func<T, IActionResult> onSuccess)
    {
        try
        {
            return onSuccess(await action());
        }
        catch (ArgumentException exception)
        {
            return Error(StatusCodes.Status400BadRequest, "Invalid request", exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Error(StatusCodes.Status409Conflict, "Request conflict", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Error(StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return Error(StatusCodes.Status404NotFound, "Resource not found", exception.Message);
        }
    }

    protected ObjectResult Error(int statusCode, string title, string detail)
        => StatusCode(statusCode, new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        });
}
