using Migrator.Domain.Models;

namespace Migrator.Domain.Interfaces;

public interface IBackupService
{
    Task<string> CreateBackupAsync(
        string connectionString,
        string? databaseName = null,
        string? migrationId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BackupRecord>> GetBackupRecordsAsync(string connectionString, CancellationToken cancellationToken = default);
}