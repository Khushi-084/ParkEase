using System.Security.Claims;

namespace Slot.API.Middleware;

/// <summary>
/// Blocks LotManagers who are not yet approved by Admin from accessing write endpoints.
/// </summary>
public class ApprovedLotManagerMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> WriteMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;

        if (user.Identity?.IsAuthenticated == true &&
            user.IsInRole("LotManager") &&
            WriteMethods.Contains(context.Request.Method))
        {
            var isApprovedClaim = user.FindFirstValue("isApproved");

            if (!string.Equals(isApprovedClaim, "True", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode  = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"error\":\"Your LotManager account is pending admin approval. You cannot perform this action yet.\"}");
                return;
            }
        }

        await next(context);
    }
}