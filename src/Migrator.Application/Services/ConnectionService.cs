namespace Migrator.Application.Services;

using Migrator.Application.DTOs;
using Migrator.Application.Interfaces;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;

public sealed class ConnectionService : IConnectionService
{
    private readonly IDatabaseProvider _databaseProvider;

    public ConnectionService(IDatabaseProvider databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    public string BuildConnectionString(ConnectionConfigurationDto config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.UseRawConnectionString)
        {
            return string.IsNullOrWhiteSpace(config.RawConnectionString)
                ? throw new ArgumentException("Raw connection string is required when UseRawConnectionString is enabled.", nameof(config))
                : config.RawConnectionString;
        }

        if (string.IsNullOrWhiteSpace(config.Server))
        {
            throw new ArgumentException("Server is required.", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.Database))
        {
            throw new ArgumentException("Database is required.", nameof(config));
        }

        return _databaseProvider.BuildConnectionString(
            config.Server,
            config.Database,
            config.AuthenticationType,
            config.Username,
            config.Password);
    }

    public Task<ConnectionTestResult> TestConnectionAsync(ConnectionConfigurationDto config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        string connectionString = BuildConnectionString(config);
        return _databaseProvider.TestConnectionAsync(connectionString, cancellationToken);
    }

    public Task<DatabaseInfo> GetDatabaseInfoAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        return _databaseProvider.GetDatabaseInfoAsync(connectionString, cancellationToken);
    }

    public string MaskConnectionString(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        return _databaseProvider.MaskConnectionString(connectionString);
    }
}