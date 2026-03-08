namespace ReleasePilot.Infrastructure.Messaging;

public sealed record KafkaSettings(
    string BootstrapServers,
    string PromotionEventsTopic,
    string AuditConsumerGroupId);

