using ReleasePilot.Domain.Enums;

namespace ReleasePilot.Application.Dtos;

public record PromotionDetailsDto(
    Guid Id,
    string ApplicationName,
    string Version,
    DeploymentEnvironment TargetEnv,
    PromotionStatus Status,
    DateTime CreatedAt,
    DateTime? LastUpdatedAt,
    IEnumerable<PromotionHistoryDto> History);

public record PromotionHistoryDto(
    PromotionStatus? FromStatus,
    PromotionStatus ToStatus,
    DateTime OccurredAt,
    string User);

public record EnvStatusDto(
    DeploymentEnvironment Environment,
    string? Version,
    PromotionStatus Status,
    DateTime? UpdatedAt);

public record PromotionSummaryDto(
    Guid Id,
    string Version,
    DeploymentEnvironment TargetEnv,
    PromotionStatus Status,
    DateTime CreatedAt);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize);