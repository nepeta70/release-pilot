using ReleasePilot.Domain.Events;

namespace ReleasePilot.Application.Ports.Repositories;

public interface IPromotionEventAuditRepository
{
    Task PersistAuditAsync(PromotionEventEnvelope env, CancellationToken cancellationToken);
}
