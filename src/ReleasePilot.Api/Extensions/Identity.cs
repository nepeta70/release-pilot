using ReleasePilot.Application.Models;

namespace ReleasePilot.Api.Extensions;

public static class IdentityExtensions
{
    public static UserIdentity GetUser(this HttpContext context)
    {
        return context.Items["UserIdentity"] as UserIdentity
            ?? throw new InvalidOperationException("UserIdentity not found in HttpContext. Ensure IdentityMiddleware is registered.");
    }
}