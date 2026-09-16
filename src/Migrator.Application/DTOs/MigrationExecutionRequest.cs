namespace Migrator.Application.DTOs;

using Migrator.Domain.Enums;

public sealed class MigrationExecutionRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool CreateBackup { get; set; }
    public string? BackupPath { get; set; }
    public bool StopOnFirstFailure { get; set; } = true;
    public EnvironmentLevel Environment { get; set; } = EnvironmentLevel.Development;
}