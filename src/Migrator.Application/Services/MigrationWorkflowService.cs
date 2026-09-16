namespace Migrator.Application.Services;

using Migrator.Application.DTOs;
using Migrator.Application.Interfaces;
using Migrator.Domain.Entities;
using Migrator.Domain.Enums;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;

public sealed class MigrationWorkflowService : IMigrationWorkflowService
{
    private readonly IDatabaseProvider _databaseProvider;
    private readonly IMigrationHistoryService _historyService;
    private readonly IConnectionService _connectionService;

    public MigrationWorkflowService(
        IDatabaseProvider databaseProvider,
        IMigrationHistoryService historyService,
        IConnectionService connectionService)
    {
        _databaseProvider = databaseProvider;
        _historyService = historyService;
        _connectionService = connectionService;
    }

    public async Task<MigrationPreviewInfo> GetPreviewInfoAsync(SqlMigrationInfo migration, string connectionString, CancellationToken cancellationToken = default)
    {
        MigrationHistoryEntry? history = await _historyService.GetByMigrationIdAsync(connectionString, migration.MigrationId, cancellationToken);

        bool isChecksumMismatch = false;
        string? warningMessage = null;
        MigrationStatus status = history?.Status ?? MigrationStatus.Pending;

        if (history is not null)
        {
            isChecksumMismatch = !string.Equals(history.Checksum, migration.Checksum, StringComparison.OrdinalIgnoreCase);
            if (isChecksumMismatch && history.Status == MigrationStatus.Applied)
            {
                warningMessage = "Applied migration has been modified and differs from what was executed.";
            }
        }

        bool wouldBeApplied = status == MigrationStatus.Pending || status == MigrationStatus.Failed;

        return new MigrationPreviewInfo
        {
            MigrationId = migration.MigrationId,
            MigrationName = migration.MigrationName,
            MigrationType = MigrationType.SqlFile,
            CurrentStatus = status,
            StoredChecksum = history?.Checksum,
            CurrentChecksum = migration.Checksum,
            IsChecksumMismatch = isChecksumMismatch,
            WouldBeApplied = wouldBeApplied,
            WarningMessage = warningMessage
        };
    }

    public async Task<MigrationPreviewInfo> GetPreviewInfoAsync(FluentMigrationInfo migration, string connectionString, CancellationToken cancellationToken = default)
    {
        string migrationId = migration.Version.ToString();
        MigrationHistoryEntry? history = await _historyService.GetByMigrationIdAsync(connectionString, migrationId, cancellationToken);

        string currentChecksum = $"{migration.Version}:{migration.Namespace}.{migration.MigrationName}";
        bool isChecksumMismatch = false;
        string? warningMessage = null;
        MigrationStatus status = history?.Status ?? MigrationStatus.Pending;

        if (history is not null)
        {
            isChecksumMismatch = !string.Equals(history.Checksum, currentChecksum, StringComparison.OrdinalIgnoreCase);
            if (isChecksumMismatch && history.Status == MigrationStatus.Applied)
            {
                warningMessage = "Applied migration has been modified and differs from what was executed.";
            }
        }

        bool wouldBeApplied = status == MigrationStatus.Pending || status == MigrationStatus.Failed;

        return new MigrationPreviewInfo
        {
            MigrationId = migrationId,
            MigrationName = migration.MigrationName,
            MigrationType = MigrationType.FluentMigrator,
            CurrentStatus = status,
            StoredChecksum = history?.Checksum,
            CurrentChecksum = currentChecksum,
            IsChecksumMismatch = isChecksumMismatch,
            WouldBeApplied = wouldBeApplied,
            WarningMessage = warningMessage
        };
    }

    public async Task<DashboardData> GetDashboardDataAsync(string connectionString, IReadOnlyList<SqlMigrationInfo> migrations, CancellationToken cancellationToken = default)
    {
        DatabaseInfo? databaseInfo = null;
        bool isConnected = false;

        try
        {
            databaseInfo = await _databaseProvider.GetDatabaseInfoAsync(connectionString, cancellationToken);
            isConnected = true;
        }
        catch
        {
            isConnected = false;
        }

        IReadOnlyList<MigrationHistoryEntry> history = isConnected
            ? await _historyService.GetAllAsync(connectionString, cancellationToken)
            : [];

        int appliedCount = 0;
        int failedCount = 0;
        int modifiedCount = 0;

        foreach (MigrationHistoryEntry entry in history)
        {
            if (entry.Status == MigrationStatus.Applied)
            {
                appliedCount++;
            }
            else if (entry.Status == MigrationStatus.Failed)
            {
                failedCount++;
            }
            else if (entry.Status == MigrationStatus.Modified)
            {
                modifiedCount++;
            }
        }

        int totalMigrations = migrations.Count;
        int appliedFromList = 0;

        foreach (SqlMigrationInfo migration in migrations)
        {
            MigrationHistoryEntry? entry = history.FirstOrDefault(h => h.MigrationId == migration.MigrationId);
            if (entry?.Status == MigrationStatus.Applied)
            {
                appliedFromList++;
            }
        }

        int pendingCount = totalMigrations - appliedFromList;

        IReadOnlyList<MigrationHistoryEntry> recentActivity = history
            .OrderByDescending(h => h.ExecutedAt)
            .Take(10)
            .ToList();

        MigrationHistoryEntry? lastMigration = history
            .OrderByDescending(h => h.ExecutedAt)
            .FirstOrDefault();

        return new DashboardData
        {
            TotalMigrations = totalMigrations,
            AppliedCount = appliedCount,
            PendingCount = pendingCount,
            FailedCount = failedCount,
            ModifiedCount = modifiedCount,
            LastMigration = lastMigration,
            IsConnected = isConnected,
            DatabaseName = databaseInfo?.DatabaseName,
            ServerName = databaseInfo?.ServerName,
            RecentActivity = recentActivity
        };
    }
}