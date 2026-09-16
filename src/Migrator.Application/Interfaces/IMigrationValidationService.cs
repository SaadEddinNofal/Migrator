namespace Migrator.Application.Interfaces;

using Migrator.Domain.Models;

public interface IMigrationValidationService
{
    Task<MigrationValidationResult> ValidateAsync(string connectionString, SqlMigrationInfo migration, CancellationToken cancellationToken = default);
    Task<MigrationValidationResult> ValidateAsync(string connectionString, FluentMigrationInfo migration, CancellationToken cancellationToken = default);
}

public sealed class MigrationValidationResult
{
    public bool IsValid { get; set; }
    public bool IsChecksumMismatch { get; set; }
    public string? StoredChecksum { get; set; }
    public string? CurrentChecksum { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> Warnings { get; set; } = [];
}