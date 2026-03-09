using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;
using Polly;

namespace ReleasePilot.Infrastructure.Messaging;

public sealed class KafkaProducerManager : IDisposable
{
    public IProducer<string, string> Producer { get; }

    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaProducerManager> _logger;

    public KafkaProducerManager(KafkaSettings settings, ILogger<KafkaProducerManager> logger)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = new ProducerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            RetryBackoffMs = 1000,
            RetryBackoffMaxMs = 2000,
            MessageSendMaxRetries = 5,
            MetadataMaxAgeMs = 180000,
            SocketTimeoutMs = 60000
        };

        Producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _settings.BootstrapServers
        }).Build();

        // Define a policy: Retry 10 times with exponential backoff (2s, 4s, 8s...)
        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(10,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, context) =>
                {
                    _logger.LogWarning("Kafka not ready: {Message}. Retrying in {Time}...",
                        exception.Message, timeSpan);
                });

        // Wrap the initialization in a global timeout (e.g. 60 seconds)
        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(60));

        await timeoutPolicy.ExecuteAsync(async () =>
        {
            await retryPolicy.ExecuteAsync(async () =>
            {
                // 1. Ensure Topic exists
                await adminClient.CreateTopicsAsync([
                    new TopicSpecification { Name = _settings.PromotionEventsTopic, ReplicationFactor = 1, NumPartitions = 1 }
                ]);

                // 2. Force Coordinator Load by fetching internal metadata
                // This is the specific "poke" that stops the GETPID spam
                adminClient.GetMetadata("__transaction_state", TimeSpan.FromSeconds(5));

                _logger.LogInformation("Kafka infrastructure is ready.");
            });
        });
    }
    public void Dispose()
    {
        Producer.Flush(TimeSpan.FromSeconds(10));
        Producer.Dispose();
    }
}