using Migrator.Domain.Entities;
using Migrator.Domain.Enums;

namespace Migrator.Domain.Interfaces;

public interface IMigrationHistoryService
{
    Task<IReadOnlyList<MigrationHistoryEntry>> GetAllAsync(string connectionString, CancellationToken cancellationToken = default);

    Task<MigrationHistoryEntry?> GetByMigrationIdAsync(string connectionString, string migrationId, CancellationToken cancellationToken = default);

    Task RecordStartAsync(string connectionString, string migrationId, string migrationName, MigrationType migrationType, string checksum, CancellationToken cancellationToken = default);

    Task RecordSuccessAsync(string connectionString, string migrationId, long executionDurationMs, CancellationToken cancellationToken = default);

    Task RecordFailureAsync(string connectionString, string migrationId, string errorMessage, long executionDurationMs, CancellationToken cancellationToken = default);
}
