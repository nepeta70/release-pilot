using ReleasePilot.Application.Dtos;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.ValueObjects;
using System.Data;

namespace ReleasePilot.Application.Ports.Repositories;

public interface IPromotionReadRepository
{
    Task<PromotionDetailsDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EnvStatusDto>> GetStatusByAppAsync(string appName);
    Task<PagedResult<PromotionSummaryDto>> ListByAppAsync(string appName, int page, int pageSize);
}

public interface IPromotionWriteRepository
{
    Task InsertAsync(Promotion promotion, IDbTransaction transaction);
    Task UpdateAsync(Promotion promotion, IDbTransaction transaction);
    Task<Promotion?> GetByIdAsync(Guid id, IDbTransaction transaction);
    Task<bool> HasInProgressAsync(
        string appName,
        DeploymentEnvironment targetEnv,
        Guid? excludePromotionId,
        IDbTransaction transaction);
}