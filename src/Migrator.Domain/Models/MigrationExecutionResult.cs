using Migrator.Domain.Enums;

namespace Migrator.Domain.Models;

public sealed class MigrationExecutionResult
{
    public bool IsSuccess { get; init; }

    public string MigrationId { get; init; } = string.Empty;

    public string MigrationName { get; init; } = string.Empty;

    public MigrationType MigrationType { get; init; }

    public long ExecutionDurationMs { get; init; }

    public string? ErrorMessage { get; init; }

    public string? BackupPath { get; init; }

    public MigrationStatus Status { get; init; }
}
