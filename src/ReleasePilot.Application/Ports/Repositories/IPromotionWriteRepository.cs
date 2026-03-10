using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using System.Data;

namespace ReleasePilot.Application.Ports.Repositories;

public interface IPromotionWriteRepository
{
    Task InsertAsync(Promotion promotion, string createdBy, IDbTransaction transaction, CancellationToken cancellationToken);
    Task UpdateAsync(Promotion promotion, string updatedBy, IDbTransaction transaction, CancellationToken cancellationToken);
    Task<Promotion?> GetByIdAsync(Guid id, IDbTransaction transaction, CancellationToken cancellationToken);
    Task<bool> HasInProgressAsync(
        string appName,
        DeploymentEnvironment targetEnv,
        Guid? excludePromotionId,
        IDbTransaction transaction, CancellationToken cancellationToken);
}
