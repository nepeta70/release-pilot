using ReleasePilot.Domain.Enums;

namespace ReleasePilot.Domain.Events;

public abstract record PromotionEvent(Guid PromotionId, DateTime OccurredOn, string ActingUser);

public record PromotionRequested(
        Guid PromotionId,
        string AppName,
        string Version,
        DeploymentEnvironment TargetEnv,
        DateTime OccurredOn,
        string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public record PromotionApproved(Guid PromotionId, string Approver, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public record DeploymentStarted(Guid PromotionId, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public record PromotionCompleted(Guid PromotionId, DateTime CompletedAt, string ActingUser)
    : PromotionEvent(PromotionId, CompletedAt, ActingUser);

public record PromotionRolledBack(Guid PromotionId, string Reason, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);

public record PromotionCancelled(Guid PromotionId, DateTime OccurredOn, string ActingUser)
    : PromotionEvent(PromotionId, OccurredOn, ActingUser);