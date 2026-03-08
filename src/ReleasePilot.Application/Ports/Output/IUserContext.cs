using ReleasePilot.Application.Models;

namespace ReleasePilot.Application.Ports.Output;

public interface IUserContext
{
    UserIdentity GetCurrent();
}