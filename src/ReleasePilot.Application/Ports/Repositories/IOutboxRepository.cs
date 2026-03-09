using ReleasePilot.Application.Models;
using System.Data;

namespace ReleasePilot.Application.Ports.Repositories;

public interface IOutboxRepository
{
    Task<IEnumerable<OutboxRow>> GetUnprocessedEventsAsync(IDbTransaction transaction, CancellationToken ct);
    Task MarkAsProcessedAsync(IEnumerable<Guid> ids, IDbTransaction transaction, CancellationToken ct);
}

public interface IOutboxUnitOfWork
{
    // The processor only cares about the high-level operation
    Task ExecuteAsync(Func<IOutboxRepository, IDbTransaction, Task> action, CancellationToken ct);
}