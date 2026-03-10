namespace ReleasePilot.Api.Contracts;

public sealed record RequestPromotionBody(
    string AppName,
    string Version,
    string TargetEnv,
    IReadOnlyList<string>? WorkItemIds);

public sealed record RollbackPromotionBody(string Reason);