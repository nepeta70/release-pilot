using MediatR;

namespace ReleasePilot.Application.DependencyInjection;

/// <summary>
/// Placeholder for future optimistic concurrency / idempotency concerns.
/// </summary>
public sealed class ConcurrencyPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => await next(cancellationToken);
}

