using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReleasePilot.Infrastructure.Messaging;

namespace ReleasePilot.Infrastructure.DependencyInjection;

public static class KafkaExtensions
{
    public static IServiceCollection AddKafkaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 3. Kafka settings
        var kafkaSection = configuration.GetSection("Kafka");
        var kafkaSettings = new KafkaSettings(
            BootstrapServers: kafkaSection["BootstrapServers"] ?? "localhost:9092",
            PromotionEventsTopic: kafkaSection["PromotionEventsTopic"] ?? "promotion-events",
            AuditConsumerGroupId: kafkaSection["AuditConsumerGroupId"] ?? "release-pilot-audit");
        services.AddSingleton(kafkaSettings);

        // This managed class handles the "Coordinator load" retries and the Flush on shutdown
        services.AddSingleton<KafkaProducerManager>();

        // Expose the interface by resolving from the manager
        services.AddSingleton(sp =>
            sp.GetRequiredService<KafkaProducerManager>().Producer);

        return services;
    }
}