using Migrator.Domain.Enums;

namespace Migrator.Domain.Models;

public sealed class MigrationPreviewInfo
{
    public string MigrationId { get; init; } = string.Empty;

    public string MigrationName { get; init; } = string.Empty;

    public MigrationType MigrationType { get; init; }

    public MigrationStatus CurrentStatus { get; init; }

    public string? StoredChecksum { get; init; }

    public string CurrentChecksum { get; init; } = string.Empty;

    public bool IsChecksumMismatch { get; init; }

    public bool WouldBeApplied { get; init; }

    public string? WarningMessage { get; init; }
}
