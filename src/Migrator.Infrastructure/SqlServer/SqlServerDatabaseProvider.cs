using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Migrator.Domain.Enums;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;

namespace Migrator.Infrastructure.SqlServer;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
    public DatabaseProviderType ProviderType => DatabaseProviderType.SqlServer;
    public string ProviderName => "SQL Server";

    public string BuildConnectionString(string server, string database, AuthenticationType authType, string? username, string? password)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = 15
        };

        switch (authType)
        {
            case AuthenticationType.Windows:
                builder.IntegratedSecurity = true;
                break;
            case AuthenticationType.SqlServer:
            case AuthenticationType.AzureActiveDirectory:
                builder.IntegratedSecurity = false;
                builder.UserID = username ?? string.Empty;
                builder.Password = password ?? string.Empty;
                break;
        }

        return builder.ConnectionString;
    }

    public string BuildConnectionString(string rawConnectionString) => rawConnectionString;

    public async Task<ConnectionTestResult> TestConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            _ = connection.ServerVersion;
            sw.Stop();

            return new ConnectionTestResult
            {
                IsSuccess = true,
                Elapsed = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ConnectionTestResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                Elapsed = sw.Elapsed
            };
        }
    }

    public async Task<DatabaseInfo> GetDatabaseInfoAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        string version = "Unknown";
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT @@VERSION";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            if (result is string s)
            {
                version = s.Split('\n').FirstOrDefault()?.Trim() ?? "Unknown";
            }
        }

        long sizeBytes = 0;
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT COALESCE(SUM(size), 0) * 8192 FROM sys.database_files";
            var result = await cmd.ExecuteScalarAsync(cancellationToken);
            sizeBytes = Convert.ToInt64(result ?? 0L, System.Globalization.CultureInfo.InvariantCulture);
        }

        return new DatabaseInfo
        {
            ServerName = connection.DataSource ?? "Unknown",
            DatabaseName = connection.Database ?? "Unknown",
            ProductVersion = version,
            SizeBytes = sizeBytes
        };
    }

    public string MaskConnectionString(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "***";
        }
        return builder.ConnectionString;
    }
}