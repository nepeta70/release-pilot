using Dapper;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Domain.Events;
using ReleasePilot.Infrastructure.Ports;

namespace ReleasePilot.Infrastructure.Adapters.Repositories;

public sealed class PromotionEventAuditRepository(IDbConnectionFactory connectionFactory) : IPromotionEventAuditRepository
{
    public async Task PersistAuditAsync(PromotionEventEnvelope env, CancellationToken cancellationToken)
    {
        using var conn = await connectionFactory.CreateConnectionAsync();
        const string sql = @"
            INSERT INTO promotion_event_audit (promotion_id, event_type, occurred_at, acting_user)
            VALUES (@PromotionId, @EventType, @OccurredOn, @ActingUser);";

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new
            {
                env.PromotionId,
                env.EventType,
                env.OccurredOn,
                env.ActingUser
            },
            cancellationToken: cancellationToken);

        await conn.ExecuteAsync(command);
    }
}