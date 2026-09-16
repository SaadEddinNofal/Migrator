namespace Migrator.Application.Services;

using System.Diagnostics;
using Migrator.Application.DTOs;
using Migrator.Application.Interfaces;
using Migrator.Domain.Entities;
using Migrator.Domain.Enums;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;
using Microsoft.Extensions.Logging;

public sealed class MigrationExecutorService
{
    private readonly IBackupService _backupService;
    private readonly IMigrationHistoryService _historyService;
    private readonly IMigrationExecutor _executor;
    private readonly IMigrationProvider _migrationProvider;
    private readonly ILogger<MigrationExecutorService> _logger;

    public MigrationExecutorService(
        IBackupService backupService,
        IMigrationHistoryService historyService,
        IMigrationExecutor executor,
        IMigrationProvider migrationProvider,
        ILogger<MigrationExecutorService> logger)
    {
        _backupService = backupService;
        _historyService = historyService;
        _executor = executor;
        _migrationProvider = migrationProvider;
        _logger = logger;
    }

    public async Task<MigrationExecutionResult> ExecuteSqlMigrationAsync(
        SqlMigrationInfo migration,
        MigrationExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migration);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConnectionString);

        string? backupPath = null;

        try
        {
            if (request.CreateBackup)
            {
                _logger.LogInformation("Creating backup before executing migration '{MigrationId}'.", migration.MigrationId);
                backupPath = await _backupService.CreateBackupAsync(request.ConnectionString, null, migration.MigrationId, cancellationToken);
            }

            await _historyService.RecordStartAsync(
                request.ConnectionString,
                migration.MigrationId,
                migration.MigrationName,
                MigrationType.SqlFile,
                migration.Checksum,
                cancellationToken);

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                await _executor.ExecuteSqlAsync(request.ConnectionString, migration.Content, cancellationToken);
                stopwatch.Stop();

                await _historyService.RecordSuccessAsync(
                    request.ConnectionString,
                    migration.MigrationId,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                _logger.LogInformation("Migration '{MigrationId}' executed successfully in {Elapsed} ms.", migration.MigrationId, stopwatch.ElapsedMilliseconds);

                return new MigrationExecutionResult
                {
                    IsSuccess = true,
                    MigrationId = migration.MigrationId,
                    MigrationName = migration.MigrationName,
                    MigrationType = MigrationType.SqlFile,
                    ExecutionDurationMs = stopwatch.ElapsedMilliseconds,
                    BackupPath = backupPath,
                    Status = MigrationStatus.Applied
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _historyService.RecordFailureAsync(
                    request.ConnectionString,
                    migration.MigrationId,
                    ex.Message,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                _logger.LogError(ex, "Migration '{MigrationId}' failed after {Elapsed} ms.", migration.MigrationId, stopwatch.ElapsedMilliseconds);

                return new MigrationExecutionResult
                {
                    IsSuccess = false,
                    MigrationId = migration.MigrationId,
                    MigrationName = migration.MigrationName,
                    MigrationType = MigrationType.SqlFile,
                    ExecutionDurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = ex.Message,
                    BackupPath = backupPath,
                    Status = MigrationStatus.Failed
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration '{MigrationId}' could not be started.", migration.MigrationId);

            return new MigrationExecutionResult
            {
                IsSuccess = false,
                MigrationId = migration.MigrationId,
                MigrationName = migration.MigrationName,
                MigrationType = MigrationType.SqlFile,
                ErrorMessage = ex.Message,
                BackupPath = backupPath,
                Status = MigrationStatus.Failed
            };
        }
    }

    public async Task<MigrationExecutionResult> ExecuteFluentMigrationAsync(
        FluentMigrationInfo migration,
        MigrationExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(migration);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ConnectionString);

        string migrationId = migration.Version.ToString();
        string checksum = $"{migration.Version}:{migration.Namespace}.{migration.MigrationName}";
        string? backupPath = null;

        try
        {
            if (request.CreateBackup)
            {
                _logger.LogInformation("Creating backup before executing FluentMigrator migration '{MigrationName}'.", migration.MigrationName);
                backupPath = await _backupService.CreateBackupAsync(request.ConnectionString, null, migrationId, cancellationToken);
            }

            await _historyService.RecordStartAsync(
                request.ConnectionString,
                migrationId,
                migration.MigrationName,
                MigrationType.FluentMigrator,
                checksum,
                cancellationToken);

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                await _migrationProvider.ExecuteFluentMigrationAsync(
                    migration.AssemblyPath,
                    migration.Version,
                    request.ConnectionString,
                    cancellationToken);

                stopwatch.Stop();

                await _historyService.RecordSuccessAsync(
                    request.ConnectionString,
                    migrationId,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                _logger.LogInformation("FluentMigrator migration '{MigrationName}' (v{Version}) executed successfully in {Elapsed} ms.", migration.MigrationName, migration.Version, stopwatch.ElapsedMilliseconds);

                return new MigrationExecutionResult
                {
                    IsSuccess = true,
                    MigrationId = migrationId,
                    MigrationName = migration.MigrationName,
                    MigrationType = MigrationType.FluentMigrator,
                    ExecutionDurationMs = stopwatch.ElapsedMilliseconds,
                    BackupPath = backupPath,
                    Status = MigrationStatus.Applied
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _historyService.RecordFailureAsync(
                    request.ConnectionString,
                    migrationId,
                    ex.Message,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                _logger.LogError(ex, "FluentMigrator migration '{MigrationName}' failed after {Elapsed} ms.", migration.MigrationName, stopwatch.ElapsedMilliseconds);

                return new MigrationExecutionResult
                {
                    IsSuccess = false,
                    MigrationId = migrationId,
                    MigrationName = migration.MigrationName,
                    MigrationType = MigrationType.FluentMigrator,
                    ExecutionDurationMs = stopwatch.ElapsedMilliseconds,
                    ErrorMessage = ex.Message,
                    BackupPath = backupPath,
                    Status = MigrationStatus.Failed
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FluentMigrator migration '{MigrationName}' could not be started.", migration.MigrationName);

            return new MigrationExecutionResult
            {
                IsSuccess = false,
                MigrationId = migrationId,
                MigrationName = migration.MigrationName,
                MigrationType = MigrationType.FluentMigrator,
                ErrorMessage = ex.Message,
                BackupPath = backupPath,
                Status = MigrationStatus.Failed
            };
        }
    }
}