namespace ReleasePilot.Domain.Enums;

public enum PromotionStatus
{
    None,
    Requested,
    Approved,
    InProgress,
    Completed,
    RolledBack,
    Cancelled
}