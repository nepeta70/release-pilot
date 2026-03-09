using ReleasePilot.Application.Dtos;

namespace ReleasePilot.Application.Ports.Repositories;

public interface IPromotionReadRepository
{
    Task<PromotionDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<EnvStatusDto>> GetStatusByAppAsync(string appName, CancellationToken cancellationToken);
    Task<PagedResult<PromotionSummaryDto>> ListByAppAsync(string appName, int page, int pageSize, CancellationToken cancellationToken);
}
