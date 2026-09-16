namespace Migrator.Application.Interfaces;

using Migrator.Application.DTOs;
using Migrator.Domain.Models;

public interface IConnectionService
{
    string BuildConnectionString(ConnectionConfigurationDto config);
    Task<ConnectionTestResult> TestConnectionAsync(ConnectionConfigurationDto config, CancellationToken cancellationToken = default);
    Task<DatabaseInfo> GetDatabaseInfoAsync(string connectionString, CancellationToken cancellationToken = default);
    string MaskConnectionString(string connectionString);
}