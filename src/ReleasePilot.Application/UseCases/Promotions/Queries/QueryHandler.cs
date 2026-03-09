using MediatR;
using ReleasePilot.Application.Dtos;
using ReleasePilot.Application.Ports.Repositories;

namespace ReleasePilot.Application.UseCases.Promotions.Queries;

public class PromotionQueryHandler(IPromotionReadRepository port) :
    IRequestHandler<GetEnvironmentStatusQuery, IEnumerable<EnvStatusDto>>,
    IRequestHandler<GetPromotionByIdQuery, PromotionDetailsDto?>,
    IRequestHandler<ListPromotionsQuery, PagedResult<PromotionSummaryDto>>
{
    public async Task<IEnumerable<EnvStatusDto>> Handle(GetEnvironmentStatusQuery request, CancellationToken ct)
        => await port.GetStatusByAppAsync(request.AppName, ct);

    public async Task<PromotionDetailsDto?> Handle(GetPromotionByIdQuery request, CancellationToken ct)
        => await port.GetByIdAsync(request.Id, ct);

    public async Task<PagedResult<PromotionSummaryDto>> Handle(ListPromotionsQuery request, CancellationToken ct)
        => await port.ListByAppAsync(request.AppName, request.Page, request.PageSize, ct);
}