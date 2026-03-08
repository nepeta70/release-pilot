using MediatR;
using ReleasePilot.Application.Dtos;

namespace ReleasePilot.Application.UseCases.Promotions.Queries;

public record GetEnvironmentStatusQuery(string AppName)
    : IRequest<IEnumerable<EnvStatusDto>>;

public record GetPromotionByIdQuery(Guid Id)
    : IRequest<PromotionDetailsDto?>;

public record ListPromotionsQuery(string AppName, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<PromotionSummaryDto>>;