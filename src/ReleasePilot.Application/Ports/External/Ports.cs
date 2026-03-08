namespace ReleasePilot.Application.Ports.External;

// Triggered when StartDeployment is called.
public interface IDeploymentPort
{
    Task<string> InitiateDeploymentAsync(string appName, string version, string targetEnv);
}

// Retrieves work item info for issue references.
public interface IIssueTrackerPort
{
    Task<IEnumerable<WorkItemDto>> GetWorkItemsAsync(IEnumerable<string> issueIds);
}

// Sends notifications for terminal states.
public interface INotificationPort
{
    Task NotifyStatusChangeAsync(Guid promotionId, string appName, string status);
}

// Result DTO for the Issue Tracker
public record WorkItemDto(string Id, string Title, string Description, string Status);