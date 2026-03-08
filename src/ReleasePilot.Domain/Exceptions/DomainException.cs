namespace ReleasePilot.Domain.Exceptions;

public class DomainException(string message) : Exception(message);

public static class PromotionErrors
{
    public const string UnauthorizedApprover = "Only a user with the Approver role can approve this promotion.";
    public const string InvalidStateTransition = "Promotion cannot transition from {0} to {1}.";
    public const string EnvironmentLocked = "The target environment is currently locked by another InProgress promotion.";
    public const string EnvironmentSkipNotAllowed = "You cannot skip environments. Required: {0}.";
    public const string PromotionNotFound = "Promotion not found.";
}