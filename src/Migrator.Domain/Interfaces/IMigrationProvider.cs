using Migrator.Domain.Models;

namespace Migrator.Domain.Interfaces;

public interface IMigrationProvider
{
    Task<IReadOnlyList<FluentMigrationInfo>> DiscoverFluentMigrationsAsync(string assemblyPath, CancellationToken cancellationToken = default);

    Task ExecuteFluentMigrationAsync(string assemblyPath, long version, string connectionString, CancellationToken cancellationToken = default);
}