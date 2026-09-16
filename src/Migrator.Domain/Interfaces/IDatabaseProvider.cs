using Migrator.Domain.Enums;
using Migrator.Domain.Models;

namespace Migrator.Domain.Interfaces;

public interface IDatabaseProvider
{
    DatabaseProviderType ProviderType { get; }

    string BuildConnectionString(string server, string database, AuthenticationType authType, string? username, string? password);

    Task<ConnectionTestResult> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default);

    Task<DatabaseInfo> GetDatabaseInfoAsync(string connectionString, CancellationToken cancellationToken = default);

    string MaskConnectionString(string connectionString);
}
