namespace ReleasePilot.Infrastructure.Messaging;

public sealed record PromotionEventEnvelope(
    Guid PromotionId,
    string EventType,
    DateTime OccurredOn,
    string ActingUser,
    string PayloadJson);

