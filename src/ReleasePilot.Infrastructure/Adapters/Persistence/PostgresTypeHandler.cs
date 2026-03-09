using Dapper;
using ReleasePilot.Domain.Enums;
using System.Data;

namespace ReleasePilot.Infrastructure.Adapters.Persistence;

public class PromotionStatusHandler : SqlMapper.TypeHandler<PromotionStatus>
{
    public override void SetValue(IDbDataParameter parameter, PromotionStatus value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.Object; // Postgres handles the string-to-enum cast
    }

    public override PromotionStatus Parse(object value)
        => Enum.Parse<PromotionStatus>(value.ToString()!);
}

public class DeploymentEnvironmentHandler : SqlMapper.TypeHandler<DeploymentEnvironment>
{
    public override void SetValue(IDbDataParameter parameter, DeploymentEnvironment value)
    {
        parameter.Value = value.ToString();
        parameter.DbType = DbType.Object; // Ensures Postgres casts the string to the 'deployment_environment' enum type
    }

    public override DeploymentEnvironment Parse(object value)
        => Enum.Parse<DeploymentEnvironment>(value.ToString()!, ignoreCase: true);
}