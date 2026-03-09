using Dapper;
using ReleasePilot.Application.Models;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Infrastructure.Ports;
using System.Data;

namespace ReleasePilot.Infrastructure.Adapters.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    public async Task<IEnumerable<OutboxRow>> GetUnprocessedEventsAsync(IDbTransaction transaction, CancellationToken ct)
    {
        const string sql = @"
            SELECT id, aggregate_id, event_type, payload::text AS payload, occurred_on
            FROM outbox_events
            WHERE processed_on IS NULL
            ORDER BY occurred_on
            LIMIT 50
            FOR UPDATE SKIP LOCKED;";

        var command = new CommandDefinition(
            sql,
            transaction: transaction,
            cancellationToken: ct);

        return await transaction.Connection.QueryAsync<OutboxRow>(command);
    }

    public async Task MarkAsProcessedAsync(IEnumerable<Guid> ids, IDbTransaction transaction, CancellationToken ct)
    {
        const string sql = @"UPDATE outbox_events SET processed_on = NOW() WHERE id = ANY(@ids);";

        var command = new CommandDefinition(
            sql,
            new { ids = ids.ToArray() },
            transaction: transaction,
            cancellationToken: ct);

        await transaction.Connection.ExecuteAsync(command);
    }
}

public sealed class OutboxUnitOfWork(
    IDbConnectionFactory connectionFactory,
    IOutboxRepository repository) : IOutboxUnitOfWork
{
    public async Task ExecuteAsync(Func<IOutboxRepository, IDbTransaction, Task> action, CancellationToken ct)
    {
        using var connection = await connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        try
        {
            await action(repository, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}