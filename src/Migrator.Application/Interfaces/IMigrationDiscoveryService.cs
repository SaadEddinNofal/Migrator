namespace Migrator.Application.Interfaces;

using Migrator.Domain.Models;

public interface IMigrationDiscoveryService
{
    Task<IReadOnlyList<SqlMigrationInfo>> DiscoverSqlMigrationsAsync(string filePath, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FluentMigrationInfo>> DiscoverFluentMigrationsAsync(string assemblyPath, CancellationToken cancellationToken = default);
}