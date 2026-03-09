using ReleasePilot.Domain.Enums;

namespace ReleasePilot.Domain.Events;

public abstract record PromotionEvent(Guid PromotionId, DateTime OccurredOn, string ActingUser);

public sealed record PromotionRequested(
        Guid PromotionId,
        string AppName,
        string Version,
        DeploymentEnvironment TargetEnv,
        DateTime OccurredOn,
        string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public sealed record PromotionApproved(Guid PromotionId, string Approver, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public sealed record DeploymentStarted(Guid PromotionId, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public sealed record PromotionCompleted(Guid PromotionId, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public sealed record PromotionRolledBack(Guid PromotionId, string Reason, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public sealed record PromotionCancelled(Guid PromotionId, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public sealed record PromotionEventEnvelope(
    Guid PromotionId,
    string EventType,
    DateTime OccurredOn,
    string ActingUser,
    string PayloadJson);