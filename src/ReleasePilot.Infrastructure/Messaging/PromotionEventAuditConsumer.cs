using Confluent.Kafka;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReleasePilot.Infrastructure.Ports;
using System.Text.Json;

namespace ReleasePilot.Infrastructure.Messaging;

/// <summary>
/// Decoupled handler: consumes promotion events and persists an audit row.
/// </summary>
public sealed class PromotionEventAuditConsumer(
    IDbConnectionFactory connectionFactory,
    KafkaSettings kafkaSettings,
    ILogger<PromotionEventAuditConsumer> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Run(() => RunLoop(stoppingToken), stoppingToken);

    private void RunLoop(CancellationToken ct)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = kafkaSettings.AuditConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(kafkaSettings.PromotionEventsTopic);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(ct);
                if (cr?.Message?.Value is null) continue;

                var env = JsonSerializer.Deserialize<PromotionEventEnvelope>(cr.Message.Value);
                if (env is null) continue;

                PersistAudit(env).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Audit consumer failed.");
                Thread.Sleep(250);
            }
        }
    }

    private async Task PersistAudit(PromotionEventEnvelope env)
    {
        using var conn = await connectionFactory.CreateConnectionAsync();

        const string sql = @"
            INSERT INTO promotion_event_audit (promotion_id, event_type, occurred_at, acting_user)
            VALUES (@PromotionId, @EventType, @OccurredOn, @ActingUser);";

        await conn.ExecuteAsync(sql, new
        {
            PromotionId = env.PromotionId,
            EventType = env.EventType,
            OccurredOn = env.OccurredOn,
            ActingUser = env.ActingUser
        });
    }
}

