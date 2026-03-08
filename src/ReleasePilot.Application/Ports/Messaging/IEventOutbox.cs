using System.Data;

namespace ReleasePilot.Application.Ports.Messaging
{
    public interface IEventOutbox
    {
        Task SaveEventAsync(object @event, Guid aggregateId, IDbTransaction transaction);
    }
}
