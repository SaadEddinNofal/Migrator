namespace Migrator.Domain.Models;

public sealed class DatabaseInfo
{
    public string ServerName { get; init; } = string.Empty;

    public string DatabaseName { get; init; } = string.Empty;

    public string ProductVersion { get; init; } = string.Empty;

    public long SizeBytes { get; init; }
}
