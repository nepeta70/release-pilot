using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Domain.Events;
using ReleasePilot.Infrastructure.Messaging;
using System.Data;
using System.Text.Json;

namespace ReleasePilot.Infrastructure.Adapters.Outbox;

/// <summary>
/// Minimal async consumer for outbox events.
/// </summary>
public sealed class OutboxProcessor(
    IOutboxUnitOfWork unitOfWork,
    IProducer<string, string> producer,
    KafkaSettings kafkaSettings,
    ILogger<OutboxProcessor> logger) : BackgroundService
{
    private readonly IOutboxUnitOfWork unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    private readonly ILogger<OutboxProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IProducer<string, string> _producer = producer ?? throw new ArgumentNullException(nameof(producer));
    private readonly KafkaSettings _kafkaSettings = kafkaSettings ?? throw new ArgumentNullException(nameof(kafkaSettings));
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await unitOfWork.ExecuteAsync(async (repo, tx) =>
                {
                    var events = (await repo.GetUnprocessedEventsAsync(tx, stoppingToken)).ToList();
                    if (events.Count == 0) return;

                    foreach (var e in events)
                    {
                        var envelope = new PromotionEventEnvelope(
                            e.aggregate_id, e.event_type, e.occurred_on, "System", e.payload);

                        await _producer.ProduceAsync(kafkaSettings.PromotionEventsTopic, new Message<string, string>
                        {
                            Key = e.aggregate_id.ToString(),
                            Value = JsonSerializer.Serialize(envelope)
                        }, stoppingToken);
                    }

                    await repo.MarkAsProcessedAsync(events.Select(x => x.id), tx, stoppingToken);
                }, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox process failed.");
            }
        }
    }
}