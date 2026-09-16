using Migrator.Domain.Enums;

namespace Migrator.Domain.Entities;

public sealed class MigrationHistoryEntry
{
    public long Id { get; set; }

    public string MigrationId { get; set; } = string.Empty;

    public string MigrationName { get; set; } = string.Empty;

    public MigrationType MigrationType { get; set; }

    public DatabaseProviderType Provider { get; set; }

    public string Checksum { get; set; } = string.Empty;

    public DateTime ExecutedAt { get; set; }

    public long ExecutionDurationMs { get; set; }

    public MigrationStatus Status { get; set; }

    public string AppliedBy { get; set; } = string.Empty;

    public string MachineName { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string DatabaseName { get; set; } = string.Empty;

    public string ServerName { get; set; } = string.Empty;
}