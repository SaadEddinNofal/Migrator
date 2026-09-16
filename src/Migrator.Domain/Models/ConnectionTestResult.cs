namespace Migrator.Domain.Models;

public sealed class ConnectionTestResult
{
    public bool IsSuccess { get; init; }

    public string? ServerName { get; init; }

    public string? DatabaseName { get; init; }

    public string? ServerVersion { get; init; }

    public string? ErrorMessage { get; init; }

    public TimeSpan Elapsed { get; init; }

    public long DurationMs => (long)Elapsed.TotalMilliseconds;
}
