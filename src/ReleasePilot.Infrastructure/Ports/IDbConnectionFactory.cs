using System.Data;

namespace ReleasePilot.Infrastructure.Ports
{
    public interface IDbConnectionFactory
    {
        Task<IDbConnection> CreateConnectionAsync();
    }
}
