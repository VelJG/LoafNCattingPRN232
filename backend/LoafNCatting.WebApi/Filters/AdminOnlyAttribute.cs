using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LoafNCatting.WebApi.Filters;

public sealed class AdminOnlyAttribute : ActionFilterAttribute
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Staff"
    };

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // ponytail: header role guard for coursework demo; replace with JWT auth when login is implemented.
        var role = context.HttpContext.Request.Headers["X-User-Role"].FirstOrDefault();
        if (role is null || !AllowedRoles.Contains(role))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Admin or staff role is required." });
        }
    }
}
