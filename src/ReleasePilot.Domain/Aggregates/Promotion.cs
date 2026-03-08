using ReleasePilot.Domain.Constants;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Domain.Events;
using ReleasePilot.Domain.Exceptions;

namespace ReleasePilot.Domain.Aggregates;

public class Promotion
{
    public Guid Id { get; }
    public string ApplicationName { get;  }
    public string Version { get; }
    public DeploymentEnvironment TargetEnvironment { get; }
    public PromotionStatus Status { get; private set; }
    public IReadOnlyList<string> WorkItemIds { get; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    public Dictionary<string, string> Metadata { get; }

    private readonly List<PromotionEvent> _domainEvents = [];
    public IReadOnlyCollection<PromotionEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Promotion(
        Guid id,
        string appName,
        string version,
        DeploymentEnvironment targetEnv,
        IReadOnlyList<string> workItemIds,
        PromotionStatus status,
        Dictionary<string, string> metadata,
        DateTime createdAt)
    {
        Id = id;
        ApplicationName = appName;
        Version = version;
        TargetEnvironment = targetEnv;
        WorkItemIds = workItemIds;
        Status = status;
        Metadata = metadata;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// 1. RequestPromotion -> Requested
    /// </summary>
    /// <returns></returns>
    public static Promotion Request(
        Guid id,
        string appName,
        string version,
        DeploymentEnvironment targetEnv,
        IReadOnlyList<string>? workItemIds = null,
        string? requestedBy = null)
    {
        if (string.IsNullOrWhiteSpace(appName)) throw new DomainException("ApplicationName is required.");
        if (string.IsNullOrWhiteSpace(version)) throw new DomainException("Version is required.");

        // Environments are fixed-order: Dev -> Staging -> Production. Promotions cannot skip.
        // Target must be the immediate next environment after source.
        var requiredPrevious = targetEnv.GetRequiredPrevious() ?? throw new DomainException("Target environment cannot be Dev.");
        
        var now = DateTime.UtcNow;
        var actor = string.IsNullOrWhiteSpace(requestedBy) ? "System" : requestedBy.Trim();
        var promotion = new Promotion(
            id,
            appName,
            version,
            targetEnv,
            workItemIds ?? [],
            PromotionStatus.Requested,
            [],
            createdAt: now);

        promotion.AddEvent(new PromotionRequested(id, appName, version, targetEnv, now, actor));
        return promotion;
    }

    public static Promotion Hydrate(
        Guid id,
        string appName,
        string version,
        DeploymentEnvironment targetEnv,
        PromotionStatus status,
        IReadOnlyList<string> workItemIds,
        Dictionary<string, string> metadata,
        DateTime createdAt,
        DateTime? updatedAt)
        => new(
            id,
            appName,
            version,
            targetEnv,
            workItemIds,
            status,
            metadata,
            createdAt);

    /// <summary>
    /// 2. ApprovePromotion -> Approved
    /// </summary>
    /// <exception cref="DomainException"></exception>
    public void Approve(string userRole, string userName, bool isTargetEnvironmentLocked)
    {
        EnsureMutable();

        if (!string.Equals(userRole, UserRoles.Approver, StringComparison.OrdinalIgnoreCase))
            throw new DomainException(PromotionErrors.UnauthorizedApprover);
        if (string.IsNullOrWhiteSpace(userName))
            throw new DomainException("Approver name is required.");
        if (Status != PromotionStatus.Requested)
            throw new DomainException(string.Format(PromotionErrors.InvalidStateTransition, Status, PromotionStatus.Approved));
        if (isTargetEnvironmentLocked)
            throw new DomainException(PromotionErrors.EnvironmentLocked);

        Status = PromotionStatus.Approved;
        AddEvent(new PromotionApproved(Id, userName, DateTime.UtcNow, userName.Trim()));
    }

    /// <summary>
    /// 3. StartDeployment -> InProgress
    /// </summary>
    /// <exception cref="DomainException"></exception>
    public void StartDeployment(bool isTargetEnvironmentLocked, string? startedBy)
    {
        EnsureMutable();

        if (Status != PromotionStatus.Approved)
            throw new DomainException(string.Format(PromotionErrors.InvalidStateTransition, Status, PromotionStatus.InProgress));
        if (isTargetEnvironmentLocked)
            throw new DomainException(PromotionErrors.EnvironmentLocked);

        var actor = string.IsNullOrWhiteSpace(startedBy) ? "System" : startedBy.Trim();
        Status = PromotionStatus.InProgress;
        AddEvent(new DeploymentStarted(Id, DateTime.UtcNow, actor));
    }

    // 4. CompletePromotion -> Completed
    public void Complete(string? completedBy)
    {
        EnsureMutable();

        if (Status != PromotionStatus.InProgress)
            throw new DomainException(string.Format(PromotionErrors.InvalidStateTransition, Status, PromotionStatus.Completed));

        Status = PromotionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        var actor = string.IsNullOrWhiteSpace(completedBy) ? "System" : completedBy.Trim();
        AddEvent(new PromotionCompleted(Id, UpdatedAt.Value, actor));
    }

    // 5. RollbackPromotion -> RolledBack
    public void Rollback(string reason, string? rolledBackBy)
    {
        EnsureMutable();

        if (Status != PromotionStatus.InProgress)
            throw new DomainException(string.Format(PromotionErrors.InvalidStateTransition, Status, PromotionStatus.RolledBack));
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rollback reason is required.");

        Status = PromotionStatus.RolledBack;
        Metadata["RollbackReason"] = reason.Trim();
        var actor = string.IsNullOrWhiteSpace(rolledBackBy) ? "System" : rolledBackBy.Trim();
        AddEvent(new PromotionRolledBack(Id, reason.Trim(), DateTime.UtcNow, actor));
    }

    // 6. CancelPromotion -> Cancelled
    public void Cancel(string? cancelledBy)
    {
        EnsureMutable();

        if (Status != PromotionStatus.Requested)
            throw new DomainException(string.Format(PromotionErrors.InvalidStateTransition, Status, PromotionStatus.Cancelled));

        var actor = string.IsNullOrWhiteSpace(cancelledBy) ? "System" : cancelledBy.Trim();
        Status = PromotionStatus.Cancelled;
        AddEvent(new PromotionCancelled(Id, DateTime.UtcNow, actor));
    }

    private void AddEvent(PromotionEvent @event) => _domainEvents.Add(@event);
    public void ClearEvents() => _domainEvents.Clear();

    private void EnsureMutable()
    {
        if (Status is PromotionStatus.Completed or PromotionStatus.Cancelled or PromotionStatus.RolledBack)
            throw new DomainException("Promotion is immutable in a terminal state.");
    }
}