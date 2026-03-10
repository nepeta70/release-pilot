using Dapper;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Domain.Aggregates;
using ReleasePilot.Domain.Enums;
using System.Data;
using System.Text.Json;

namespace ReleasePilot.Infrastructure.Adapters.Repositories;

public class PromotionWriteRepository(IDbConnection connection) : IPromotionWriteRepository
{
    public async Task InsertAsync(Promotion promotion, string createdBy, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO promotions (
                id, application_name, version, target_env, current_status, work_items, metadata, created_at, updated_at, last_updated_by
            ) VALUES (
                @Id, @AppName, @Version, @TargetEnv, 
                CAST(@Status AS promotion_status),
                CAST(@WorkItems AS jsonb), 
                CAST(@Metadata AS jsonb), 
                NOW(), 
                NOW(),
                @CreatedBy
            );";

        var command = new CommandDefinition(
                    sql,
                    new
                    {
                        Id =  promotion.Id,
                        AppName= promotion.ApplicationName,
                        Version= promotion.Version,
                        TargetEnv= promotion.TargetEnvironment.ToString(),
                        Status= promotion.Status.ToString(),
                        WorkItems= JsonSerializer.Serialize(promotion.WorkItems),
                        Metadata= JsonSerializer.Serialize(promotion.Metadata),
                        CreatedBy = createdBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task UpdateAsync(Promotion promotion, string updatedBy, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE promotions SET 
                current_status = CAST(@Status AS promotion_status),
                metadata = CAST(@Metadata AS jsonb),
                updated_at = NOW(),
                last_updated_by = @UpdatedBy
            WHERE id = @Id;";

        var command = new CommandDefinition(
                    sql,
                    new
                    {
                        promotion.Id,
                        Status = promotion.Status.ToString(),
                        Metadata = JsonSerializer.Serialize(promotion.Metadata),
                        UpdatedBy = updatedBy
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<Promotion?> GetByIdAsync(Guid id, IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
                SELECT
                    id,
                    application_name,
                    version,
                    target_env,
                    current_status,
                    work_items,
                    metadata,
                    created_at,
                    updated_at
                FROM promotions
                WHERE id = @id;";

        var command = new CommandDefinition(
                    sql,
                    new { id },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<PromotionRow>(command);
        if (row is null) return null;

        var workItems = ParseWorkItems(row.work_items);
        var metadata = string.IsNullOrEmpty(row.metadata)
                ? []
                : JsonSerializer.Deserialize<Dictionary<string, string>>(row.metadata)!;

        return Promotion.Hydrate(
            row.id,
            row.application_name,
            row.version,
            Enum.Parse<DeploymentEnvironment>(row.target_env, ignoreCase: true),
            Enum.Parse<PromotionStatus>(row.current_status, ignoreCase: true),
            workItems,
            metadata,
            row.created_at,
            row.updated_at);
    }

    public async Task<bool> HasInProgressAsync(
        string appName,
        DeploymentEnvironment targetEnv,
        Guid? excludePromotionId,
        IDbTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = @"
                SELECT EXISTS (
                    SELECT 1
                    FROM promotions
                    WHERE application_name = @appName
                      AND target_env = @targetEnv
                      AND current_status = 'InProgress'
                      AND (@excludePromotionId IS NULL OR id <> @excludePromotionId)
                );";

        var command = new CommandDefinition(
                    sql,
                    new
                    {
                        appName,
                        targetEnv = targetEnv.ToString(),
                        excludePromotionId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }



    private static IReadOnlyList<string> ParseWorkItems(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private sealed record PromotionRow
    {
        public Guid id { get; init; }
        public string application_name { get; init; } = null!;
        public string version { get; init; } = null!;
        public string target_env { get; init; } = null!;
        public string current_status { get; init; } = null!;
        public string? work_items { get; init; }
        public string? metadata { get; init; }
        public DateTime created_at { get; init; }
        public DateTime? updated_at { get; init; }
    }
}
