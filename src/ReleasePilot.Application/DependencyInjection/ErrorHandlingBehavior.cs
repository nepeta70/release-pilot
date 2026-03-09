using MediatR;

namespace ReleasePilot.Application.DependencyInjection;

/// <summary>
/// Central place to ensure domain errors flow to the API layer.
/// The API maps DomainException to non-500 responses.
/// </summary>
public sealed class ErrorHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
        => await next(cancellationToken);
}

