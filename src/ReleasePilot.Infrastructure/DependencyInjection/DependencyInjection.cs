using Confluent.Kafka;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using ReleasePilot.Application.Adapters.External;
using ReleasePilot.Application.Ports.External;
using ReleasePilot.Application.Ports.Messaging;
using ReleasePilot.Application.Ports.Output;
using ReleasePilot.Application.Ports.Repositories;
using ReleasePilot.Domain.Enums;
using ReleasePilot.Infrastructure.Adapters.Outbox;
using ReleasePilot.Infrastructure.Adapters.Persistence;
using ReleasePilot.Infrastructure.Adapters.Repositories;
using ReleasePilot.Infrastructure.Identity;
using ReleasePilot.Infrastructure.Messaging;
using ReleasePilot.Infrastructure.Ports;
using System.Data;
namespace ReleasePilot.Infrastructure.DependencyInjection;

public static class PostgresSetup
{
    public static void MapPostgresTypes(string connectionString)
    {
        // Tell Npgsql to map the DB Enum to our Domain Enum
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.MapEnum<PromotionStatus>("promotion_status");

        // This ensures Dapper understands the translation
        SqlMapper.AddTypeHandler(new PromotionStatusHandler());
    }
}

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Database Connection (Postgres)
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        PostgresSetup.MapPostgresTypes(connectionString);

        // Using NpgsqlConnection for Dapper
        services.AddScoped<IDbConnection>(_ => new NpgsqlConnection(connectionString));
        services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IUserContext, UserContextAdapter>();

        // 2. Repositories (Separated Read/Write as requested)
        services.AddScoped<IPromotionWriteRepository, PromotionWriteRepository>();
        services.AddScoped<IPromotionReadRepository, PromotionReadRepository>();
        services.AddScoped<IPromotionEventAuditRepository, PromotionEventAuditRepository>();

        // 3. Kafka settings
        services.AddKafkaInfrastructure(configuration);

        // 5. Outbox (event-driven async publishing)
        services.AddScoped<IEventOutbox, DapperEventOutbox>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IOutboxUnitOfWork, OutboxUnitOfWork>();
        services.AddSingleton<IHostedService>(sp =>
        {
            var scope = sp.CreateScope();

            // Resolve the dependencies
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IOutboxUnitOfWork>();
            var producer = sp.GetRequiredService<IProducer<string, string>>();
            var settings = sp.GetRequiredService<KafkaSettings>();
            var logger = sp.GetRequiredService<ILogger<OutboxProcessor>>();

            return new OutboxProcessor(unitOfWork, producer, settings, logger);
        });
        services.AddSingleton<IHostedService>(sp =>
        {
            // Create a dedicated scope for this consumer
            var scope = sp.CreateScope();

            // Resolve the dependencies required by your constructor
            var auditRepository = scope.ServiceProvider.GetRequiredService<IPromotionEventAuditRepository>();
            var kafkaSettings = sp.GetRequiredService<KafkaSettings>();
            var logger = sp.GetRequiredService<ILogger<PromotionEventAuditConsumer>>();

            // Return your class with the repo injected directly
            return new PromotionEventAuditConsumer(auditRepository, kafkaSettings, logger);
        });

        // 6. External System Ports (Stubs/Adapters)
        services.AddScoped<IDeploymentPort, StubDeploymentAdapter>();
        services.AddScoped<IIssueTrackerPort, StubIssueTrackerAdapter>();
        services.AddScoped<INotificationPort, StubNotificationAdapter>();

        // 4. Kafka Outbox / Producers (Stub for now or real Confluent implementation)
        // services.AddSingleton<IKafkaProducer, KafkaProducer>();


        return services;
    }
}