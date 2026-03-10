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