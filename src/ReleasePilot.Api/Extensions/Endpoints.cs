using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReleasePilot.Api.Contracts;
using ReleasePilot.Application.UseCases.Promotions.Commands;
using ReleasePilot.Application.UseCases.Promotions.Queries;

namespace ReleasePilot.Api.Extensions;

public static class PromotionEndpoints
{
    public static IEndpointRouteBuilder MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/promotions");

        group.MapPut("/", async (RequestPromotionBody body, ISender sender, CancellationToken ct, HttpContext context) =>
        {
            var id = await sender.Send(new RequestPromotionCommand(
                body.AppName, body.Version, body.TargetEnv, body.WorkItemIds), ct);

            return Results.Created($"/promotions/{id}", new { id });
        });

        group.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct, HttpContext context) =>
        {
            await sender.Send(new ApprovePromotionCommand(id), ct);
            return Results.Accepted();
        });

        group.MapPost("/{id:guid}/start", async (Guid id, ISender sender, CancellationToken ct, HttpContext context) =>
        {
            await sender.Send(new StartDeploymentCommand(id), ct);
            return Results.Accepted();
        });

        group.MapPost("/{id:guid}/complete", async (Guid id, ISender sender, CancellationToken ct, HttpContext context) =>
        {
            await sender.Send(new CompletePromotionCommand(id), ct);
            return Results.Accepted();
        });

        group.MapPost("/{id:guid}/rollback", async (Guid id, [FromBody] RollbackPromotionBody body, ISender sender, CancellationToken ct, HttpContext context) =>
        {
            await sender.Send(new RollbackPromotionCommand(id, body.Reason), ct);
            return Results.Accepted();
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, ISender sender, CancellationToken ct, HttpContext context) =>
        {
            await sender.Send(new CancelPromotionCommand(id), ct);
            return Results.Accepted();
        });

        // Queries
        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetPromotionByIdQuery(id), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapGet("/{application}/status", async (string application, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetEnvironmentStatusQuery(application), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        group.MapGet("/{application}/list/{page}/{pageSize}", async (string application, ISender sender, CancellationToken ct, int page = 1, int pageSize = 10) =>
        {
            var dto = await sender.Send(new ListPromotionsQuery(application, page, pageSize), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        });

        return app;
    }
}