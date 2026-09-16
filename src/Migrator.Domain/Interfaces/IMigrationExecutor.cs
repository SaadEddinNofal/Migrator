namespace Migrator.Domain.Interfaces;

public interface IMigrationExecutor
{
    Task ExecuteSqlAsync(string connectionString, string sqlContent, CancellationToken cancellationToken = default);
}
