using Microsoft.AspNetCore.Http;
using ReleasePilot.Application.Models;
using ReleasePilot.Application.Ports.Output;

namespace ReleasePilot.Infrastructure.Identity;

public class UserContextAdapter(IHttpContextAccessor accessor) : IUserContext
{
    public UserIdentity GetCurrent()
    {
        // We cast the item injected by your Middleware
        var identity = accessor.HttpContext?.Items["UserIdentity"] as UserIdentity;

        // Fail fast if the adapter is called but middleware didn't run
        return identity ?? throw new UnauthorizedAccessException("User identity could not be resolved from the current request.");
    }
}
