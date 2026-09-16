namespace Migrator.Application.DTOs;

using Migrator.Domain.Entities;
using Migrator.Domain.Enums;

public sealed class DashboardData
{
    public int TotalMigrations { get; set; }
    public int AppliedCount { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int ModifiedCount { get; set; }
    public MigrationHistoryEntry? LastMigration { get; set; }
    public bool IsConnected { get; set; }
    public string? DatabaseName { get; set; }
    public string? ServerName { get; set; }
    public EnvironmentLevel Environment { get; set; }
    public IReadOnlyList<MigrationHistoryEntry> RecentActivity { get; set; } = [];
}