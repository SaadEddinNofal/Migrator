namespace Migrator.Domain.Models;

public sealed class BackupRecord
{
    public long Id { get; set; }

    public string BackupName { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public long SizeBytes { get; set; }

    public string Status { get; set; } = "Success";

    public string? MigrationId { get; set; }

    public string Location { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}