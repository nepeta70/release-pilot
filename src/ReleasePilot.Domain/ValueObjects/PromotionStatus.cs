namespace ReleasePilot.Domain.ValueObjects;

public enum PromotionStatus
{
    Requested,
    Approved,
    InProgress,
    Completed,
    RolledBack,
    Cancelled
}