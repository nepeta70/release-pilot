using MediatR;

namespace ReleasePilot.Application.UseCases.Promotions.Commands;

public record RequestPromotionCommand(
    string AppName,
    string Version,
    string TargetEnv,
    IReadOnlyList<string>? WorkItemIds = null) : IRequest<Guid>;
public record ApprovePromotionCommand(Guid PromotionId) : IRequest;
public record StartDeploymentCommand(Guid PromotionId) : IRequest;
public record CompletePromotionCommand(Guid PromotionId) : IRequest;
public record RollbackPromotionCommand(Guid PromotionId, string Reason) : IRequest;
public record CancelPromotionCommand(Guid PromotionId) : IRequest;