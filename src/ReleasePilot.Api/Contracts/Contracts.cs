namespace ReleasePilot.Api.Contracts;

public sealed record RequestPromotionBody(
    string AppName,
    string Version,
    string SourceEnv,
    string TargetEnv,
    IReadOnlyList<string>? WorkItemIds,
    string? RequestedBy);

public sealed record RollbackPromotionBody(string Reason);