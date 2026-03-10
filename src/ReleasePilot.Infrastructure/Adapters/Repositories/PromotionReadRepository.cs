using Dapper;
using ReleasePilot.Application.Dtos;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Domain.Enums;
using System.Data;

namespace ReleasePilot.Infrastructure.Adapters.Repositories;

public class PromotionReadRepository(IDbConnection connection) : IPromotionReadRepository
{
    public async Task<PromotionDetailsDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT 
                p.id, p.application_name, p.version, p.target_env, p.current_status, p.created_at, p.updated_at,
                l.from_status, l.to_status, l.occurred_at, l.acting_user
            FROM promotions p
            LEFT JOIN promotion_audit_logs l ON p.id = l.promotion_id
            WHERE p.id = @id
            ORDER BY l.occurred_at ASC;
            ";

        PromotionDetailsDto? result = null;

        var command = new CommandDefinition(
        commandText: sql,
        parameters: new { id },
        cancellationToken: cancellationToken);

        await connection.QueryAsync<PromotionReadEntity, AuditLogEntity, PromotionDetailsDto?>(
            command: command,
            map: (p, l) =>
            {
                result ??= new PromotionDetailsDto(
                    p.id,
                    p.application_name,
                    p.version,
                    Enum.Parse<DeploymentEnvironment>(p.target_env, ignoreCase: true),
                    Enum.Parse<PromotionStatus>(p.current_status, ignoreCase: true),
                    p.created_at,
                    p.updated_at,
                    []);

                if (l != null && l.to_status != null)
                {
                    // Cast to List to allow appending history items from the join
                    if (result.History is List<PromotionHistoryDto> historyList)
                    {
                        historyList.Add(new PromotionHistoryDto(
                            l.from_status != null ? Enum.Parse<PromotionStatus>(l.from_status, ignoreCase: true) : null,
                            Enum.Parse<PromotionStatus>(l.to_status, ignoreCase: true),
                            l.occurred_at,
                            l.acting_user ?? "System"));
                    }
                }
                return null;
            },
            splitOn: "from_status"
        );

        return result;
    }

    public async Task<IEnumerable<EnvStatusDto>> GetStatusByAppAsync(string appName, CancellationToken cancellationToken)
    {
        // Dashboard-style: always return Dev/Staging/Production, even if empty.
        // Promotions only target Staging/Production, but the caller expects all environments.
        const string sql = @"
            WITH latest AS (
                SELECT DISTINCT ON (p.target_env)
                    p.target_env AS env,
                    p.version,
                    p.current_status::text AS status,
                    p.updated_at
                FROM promotions p
                WHERE p.application_name = @appName
                ORDER BY p.target_env, p.updated_at DESC
            )
            SELECT
                envs.name AS Environment,
                latest.version AS Version,
                COALESCE(latest.status, 'None') AS Status,
                latest.updated_at AS UpdatedAt
            FROM deployment_environments envs
            LEFT JOIN latest ON latest.env = envs.name
            ORDER BY envs.sort_order;";

        var command = new CommandDefinition(
                    sql,
                    new { appName },
                    cancellationToken: cancellationToken);

        var res = await connection.QueryAsync<EnvStatusEntity>(command);
        return res.Select(e => new EnvStatusDto(
            Environment: Enum.Parse<DeploymentEnvironment>(e.Environment, ignoreCase: true),
            Version: e.Version,
            Status: Enum.Parse<PromotionStatus>(e.Status, ignoreCase: true),
            UpdatedAt: e.UpdatedAt));

    }

    public async Task<PagedResult<PromotionSummaryDto>> ListByAppAsync(string appName, int page, int pageSize, CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, version, target_env as TargetEnv, current_status as Status, created_at as CreatedAt
            FROM promotions
            WHERE application_name = @appName
            ORDER BY created_at DESC
            LIMIT @pageSize OFFSET @offset;

            SELECT COUNT(*) FROM promotions WHERE application_name = @appName;
            ";

        var command = new CommandDefinition(
                    sql,
                    new
                    {
                        appName,
                        pageSize,
                        offset = (page - 1) * pageSize
                    },
                    cancellationToken: cancellationToken);

        using var multi = await connection.QueryMultipleAsync(command);

        var rows = await multi.ReadAsync<PromotionSummaryEntity>();
        var total = await multi.ReadFirstAsync<int>();

        var items = rows.Select(r => new PromotionSummaryDto(
            Id: r.Id,
            Version: r.Version,
            TargetEnv: Enum.Parse<DeploymentEnvironment>(r.TargetEnv, ignoreCase: true),
            Status: Enum.Parse<PromotionStatus>(r.Status, ignoreCase: true),
            CreatedAt: r.CreatedAt));

        return new PagedResult<PromotionSummaryDto>(items, total, page, pageSize);
    }
}

internal record PromotionReadEntity
{
    public Guid id { get; init; }
    public string application_name { get; init; } = null!;
    public string version { get; init; } = null!;
    public string target_env { get; init; } = null!;
    public string current_status { get; init; } = null!;
    public string? work_items { get; init; }
    public string? metadata { get; init; }
    public DateTime created_at { get; init; }
    public DateTime updated_at { get; init; }
}

internal record AuditLogEntity
{
    public long id { get; init; }
    public Guid promotion_id { get; init; }
    public string? from_status { get; init; }
    public string to_status { get; init; } = null!;
    public string? acting_user { get; init; }
    public DateTime occurred_at { get; init; }
}

internal record EnvStatusEntity(
    string Environment,
    string? Version,
    string Status,
    DateTime? UpdatedAt);

internal record PromotionSummaryEntity(
    Guid Id,
    string Version,
    string TargetEnv,
    string Status,
    DateTime CreatedAt);