using ReleasePilot.Domain.Events;
using System.Data;

namespace ReleasePilot.Application.Ports.Messaging
{
    public interface IEventOutbox
    {
        Task SaveEventAsync(PromotionEvent @event, Guid aggregateId, IDbTransaction transaction, CancellationToken cancellationToken);
    }
}
