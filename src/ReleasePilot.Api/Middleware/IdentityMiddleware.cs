using ReleasePilot.Application.Models;

namespace ReleasePilot.Api.Middleware;

public class IdentityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var name = context.Request.Headers["X-User-Name"].ToString();
        var role = context.Request.Headers["X-Role"].ToString();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(role))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Identity headers are missing.");
            return;
        }

        // Inject into Items for downstream retrieval
        context.Items["UserIdentity"] = new UserIdentity(name, role);

        await next(context);
    }
}