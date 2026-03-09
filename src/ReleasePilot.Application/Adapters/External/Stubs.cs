using Microsoft.Extensions.Logging;
using ReleasePilot.Application.Ports.External;

namespace ReleasePilot.Application.Adapters.External;

public class StubDeploymentAdapter(ILogger<StubDeploymentAdapter> logger) : IDeploymentPort
{
    public async Task<string> InitiateDeploymentAsync(string appName, string version, string targetEnv)
    {
        logger.LogInformation("STUB: Initiating deployment for {App} v{Version} to {Env}", appName, version, targetEnv);
        await Task.Delay(100); // Simulate network
        return $"DEPLOY-{Guid.NewGuid().ToString()[..8].ToUpper()}";
    }
}

public class StubIssueTrackerAdapter : IIssueTrackerPort
{
    public Task<IEnumerable<WorkItemDto>> GetWorkItemsAsync(IEnumerable<string> issueIds)
    {
        var items = issueIds.Select(id => new WorkItemDto(
            id,
            $"Feature {id}",
            "Simulated description from external tracker.",
            "Closed"));

        return Task.FromResult(items);
    }
}

public class StubNotificationAdapter(ILogger<StubNotificationAdapter> logger) : INotificationPort
{
    public Task NotifyStatusChangeAsync(Guid promotionId, string appName, string status)
    {
        logger.LogInformation("STUB: Notification sent for Promotion {Id} ({App}). New Status: {Status}",
            promotionId, appName, status);
        return Task.CompletedTask;
    }
}