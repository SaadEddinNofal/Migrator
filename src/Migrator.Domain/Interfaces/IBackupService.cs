namespace Migrator.Domain.Interfaces;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string connectionString, string databaseName, CancellationToken cancellationToken = default);
}
