using Dapper;
using ReleasePilot.Application.Ports.Messaging;
using ReleasePilot.Domain.Events;
using System.Data;
using System.Text.Json;

namespace ReleasePilot.Infrastructure.Adapters.Outbox;

public sealed class DapperEventOutbox(IDbConnection connection) : IEventOutbox
{
    private const string insertSql = @"
            INSERT INTO outbox_events (id, aggregate_id, event_type, payload, occurred_on)
            VALUES (@Id, @AggregateId, @EventType, CAST(@Payload AS jsonb), @OccurredOn);";
    public async Task SaveEventAsync(PromotionEvent @event, Guid aggregateId, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        var type = @event.GetType();
        var envelope = new OutboxEnvelope(
            Id: Guid.NewGuid(),
            AggregateId: aggregateId,
            EventType: type.FullName ?? type.Name,
            Payload: JsonSerializer.Serialize(@event, type),
            OccurredOn: @event.OccurredOn);

        var command = new CommandDefinition(
            commandText: insertSql,
            parameters: envelope,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    private sealed record OutboxEnvelope(
        Guid Id,
        Guid AggregateId,
        string EventType,
        string Payload,
        DateTime OccurredOn);
}

