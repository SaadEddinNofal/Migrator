namespace Migrator.Application.DTOs;

using Migrator.Domain.Enums;

public sealed class ConnectionConfigurationDto
{
    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public AuthenticationType AuthenticationType { get; set; } = AuthenticationType.Windows;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? RawConnectionString { get; set; }
    public bool UseRawConnectionString { get; set; }
    public DatabaseProviderType Provider { get; set; } = DatabaseProviderType.SqlServer;
}