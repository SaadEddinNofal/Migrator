namespace Migrator.Application.Services;

using Migrator.Application.Interfaces;
using Migrator.Domain.Entities;
using Migrator.Domain.Enums;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;

public sealed class MigrationValidationService : IMigrationValidationService
{
    private readonly IMigrationHistoryService _historyService;
    private readonly IChecksumService _checksumService;

    public MigrationValidationService(
        IMigrationHistoryService historyService,
        IChecksumService checksumService)
    {
        _historyService = historyService;
        _checksumService = checksumService;
    }

    public async Task<MigrationValidationResult> ValidateAsync(string connectionString, SqlMigrationInfo migration, CancellationToken cancellationToken = default)
    {
        MigrationHistoryEntry? existing = await _historyService.GetByMigrationIdAsync(connectionString, migration.MigrationId, cancellationToken);

        return Validate(migration.MigrationId, migration.MigrationName, migration.Checksum, existing);
    }

    public async Task<MigrationValidationResult> ValidateAsync(string connectionString, FluentMigrationInfo migration, CancellationToken cancellationToken = default)
    {
        string migrationId = migration.Version.ToString();
        string currentChecksum = ComputeFluentChecksum(migration);

        MigrationHistoryEntry? existing = await _historyService.GetByMigrationIdAsync(connectionString, migrationId, cancellationToken);

        return Validate(migrationId, migration.MigrationName, currentChecksum, existing);
    }

    private MigrationValidationResult Validate(string migrationId, string migrationName, string currentChecksum, MigrationHistoryEntry? existing)
    {
        if (existing is null)
        {
            return new MigrationValidationResult { IsValid = true };
        }

        bool isSameChecksum = string.Equals(existing.Checksum, currentChecksum, StringComparison.OrdinalIgnoreCase);

        MigrationValidationResult result = new()
        {
            IsValid = isSameChecksum,
            IsChecksumMismatch = !isSameChecksum,
            StoredChecksum = existing.Checksum,
            CurrentChecksum = currentChecksum,
            ErrorMessage = !isSameChecksum
                ? $"Checksum mismatch for migration '{migrationId}'. Stored: {existing.Checksum}, Current: {currentChecksum}."
                : null
        };

        if (existing.Status == MigrationStatus.Failed)
        {
            result.IsValid = true;
            result.IsChecksumMismatch = false;
            result.StoredChecksum = null;
            result.CurrentChecksum = null;
            result.ErrorMessage = null;
            result.Warnings.Add(
                $"The migration '{migrationName}' previously failed and will be re-executed. Previous error: {existing.ErrorMessage}");
        }

        if (!isSameChecksum && existing.Status == MigrationStatus.Applied)
        {
            result.Warnings.Add($"The applied migration '{migrationName}' has been modified since it was executed.");
        }

        return result;
    }

    private string ComputeFluentChecksum(FluentMigrationInfo migration)
    {
        string content = $"{migration.Version}:{migration.Namespace}.{migration.MigrationName}";
        return _checksumService.ComputeChecksum(content);
    }
}