using Npgsql;
using ReleasePilot.Api.Extensions;
using ReleasePilot.Application.DependencyInjection;
using ReleasePilot.Domain.Exceptions;
using ReleasePilot.Infrastructure;
using ReleasePilot.Infrastructure.DependencyInjection;
using ReleasePilot.Infrastructure.Messaging;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Apply DB migrations (idempotent SQL scripts embedded in Infrastructure).
var cs = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(cs))
{
    DatabaseMigrator.EnsureDatabase(cs);
}

// Exception-to-HTTP mapping: domain rule violations must not be 500s.
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (DomainException ex)
    {
        ctx.Response.StatusCode = ex.Message switch
        {
            PromotionErrors.EnvironmentLocked => StatusCodes.Status409Conflict,
            PromotionErrors.UnauthorizedApprover => StatusCodes.Status403Forbidden,
            PromotionErrors.PromotionNotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505" && string.Equals(ex.ConstraintName, "uidx_promotion_lock", StringComparison.OrdinalIgnoreCase))
    {
        ctx.Response.StatusCode = StatusCodes.Status409Conflict;
        await ctx.Response.WriteAsJsonAsync(new { error = PromotionErrors.EnvironmentLocked });
    }
    catch (ArgumentException ex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPromotionEndpoints();

var kafkaManager = app.Services.GetRequiredService<KafkaProducerManager>();
await kafkaManager.InitializeAsync(CancellationToken.None);

app.Run();