using Microsoft.Extensions.Configuration;
using Npgsql;
using ReleasePilot.Infrastructure.Ports;
using System.Data;

namespace ReleasePilot.Infrastructure.Adapters.Persistence;

public class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    public async Task<IDbConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        return connection;
    }
}

