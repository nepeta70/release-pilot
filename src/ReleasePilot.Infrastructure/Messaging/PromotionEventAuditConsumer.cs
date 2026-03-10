using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Domain.Events;
using System.Text.Json;

namespace ReleasePilot.Infrastructure.Messaging;

public sealed class PromotionEventAuditConsumer(
    IPromotionEventAuditRepository _auditRepository,
    KafkaSettings kafkaSettings,
    ILogger<PromotionEventAuditConsumer> logger) : BackgroundService
{
    private readonly ILogger<PromotionEventAuditConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            GroupId = kafkaSettings.AuditConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = true
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(kafkaSettings.PromotionEventsTopic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);

                if (result?.Message?.Value is null) continue;

                var envelope = JsonSerializer.Deserialize<PromotionEventEnvelope>(result.Message.Value);
                if (envelope is null) continue;

                await _auditRepository.PersistAuditAsync(envelope, stoppingToken);
            }
            catch (ConsumeException ex) when (ex.Error.Code == ErrorCode.UnknownTopicOrPart)
            {
                _logger.LogWarning("Topic {Topic} is being created or not yet visible. Retrying...", kafkaSettings.PromotionEventsTopic);
                await Task.Delay(2000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Audit consumer loop error. Retrying in 1s...");
                await Task.Delay(1000, stoppingToken);
            }
        }

        consumer.Close();
    }
}
