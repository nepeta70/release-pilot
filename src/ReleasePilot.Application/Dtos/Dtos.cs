using ReleasePilot.Domain.Enums;

namespace ReleasePilot.Application.Dtos;

public record PromotionDetailsDto(
    Guid Id,
    string ApplicationName,
    string Version,
    DeploymentEnvironment TargetEnv,
    string Status,
    DateTime CreatedAt,
    IEnumerable<PromotionHistoryDto> History);

public record PromotionHistoryDto(
    string FromStatus,
    string ToStatus,
    DateTime OccurredAt,
    string User);

public record EnvStatusDto(
    DeploymentEnvironment Environment,
    string? Version,
    string Status,
    DateTime? UpdatedAt);

public record PromotionSummaryDto(
    Guid Id,
    string Version,
    DeploymentEnvironment TargetEnv,
    string Status,
    DateTime CreatedAt);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize);