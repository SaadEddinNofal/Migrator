using Microsoft.Extensions.DependencyInjection;
using Migrator.Domain.Interfaces;
using Migrator.Infrastructure.Backup;
using Migrator.Infrastructure.Checksum;
using Migrator.Infrastructure.Configuration;
using Migrator.Infrastructure.FluentMigrator;
using Migrator.Infrastructure.History;
using Migrator.Infrastructure.SqlServer;

namespace Migrator.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMigratorInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDatabaseProvider, SqlServerDatabaseProvider>();
        services.AddSingleton<IChecksumService, Sha256ChecksumService>();
        services.AddSingleton<ISqlBatchParser, SqlServerBatchParser>();
        services.AddSingleton<IMigrationHistoryService, SqlServerMigrationHistoryService>();
        services.AddSingleton<IBackupService, SqlServerBackupService>();
        services.AddSingleton<IMigrationProvider, FluentMigratorMigrationProvider>();
        services.AddSingleton<IMigrationExecutor, SqlServerMigrationExecutor>();
        services.AddSingleton<UserSettingsService>();
        return services;
    }
}