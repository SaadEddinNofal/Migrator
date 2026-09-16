using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;

namespace Migrator.Infrastructure.Backup;

public sealed class SqlServerBackupService : IBackupService
{
    private const string EnsureTableSql = """
        IF OBJECT_ID(N'dbo.__MigratorBackups', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.__MigratorBackups (
                Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MigratorBackups PRIMARY KEY,
                BackupName NVARCHAR(256) NOT NULL,
                DatabaseName NVARCHAR(128) NOT NULL,
                CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MigratorBackups_CreatedAt DEFAULT (SYSUTCDATETIME()),
                SizeBytes BIGINT NOT NULL CONSTRAINT DF_MigratorBackups_SizeBytes DEFAULT (0),
                Status NVARCHAR(50) NOT NULL,
                MigrationId NVARCHAR(256) NULL,
                Location NVARCHAR(512) NOT NULL,
                ErrorMessage NVARCHAR(MAX) NULL
            );

            CREATE INDEX IX_MigratorBackups_CreatedAt ON dbo.__MigratorBackups (CreatedAt);
        END
        """;

    private const string RecordBackupSql = """
        INSERT INTO dbo.__MigratorBackups (
            BackupName, DatabaseName, CreatedAt, SizeBytes, Status, MigrationId, Location, ErrorMessage)
        VALUES (
            @BackupName, @DatabaseName, SYSUTCDATETIME(), @SizeBytes, @Status, @MigrationId, @Location, @ErrorMessage);
        """;

    private const string SelectRecordSql = """
        SELECT Id, BackupName, DatabaseName, CreatedAt, SizeBytes, Status, MigrationId, Location, ErrorMessage
        FROM dbo.__MigratorBackups
        ORDER BY CreatedAt DESC, Id DESC;
        """;

    public async Task<string?> CreateBackupAsync(string connectionString, string? databaseName = null, string? migrationId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!IsLocalServer(connectionString))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        }

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("A database name could not be determined from the connection string.");
        }

        string backupPath = ResolveBackupPath(databaseName);

        string? directory = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        const string commandText = """
            BACKUP DATABASE @DatabaseName
            TO DISK = @BackupPath
            WITH INIT, COMPRESSION;
            """;

        var sw = Stopwatch.StartNew();
        try
        {
            await using var executorConnection = new SqlConnection(connectionString);
            await executorConnection.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand(commandText, executorConnection);
            cmd.Parameters.AddWithValue("@DatabaseName", databaseName);
            cmd.Parameters.AddWithValue("@BackupPath", backupPath);
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            long sizeBytes = GetBackupFileSize(backupPath, connectionString, cancellationToken);
            sw.Stop();

            await RecordBackupAsync(
                connectionString,
                Path.GetFileName(backupPath),
                databaseName,
                sizeBytes,
                "Success",
                migrationId,
                backupPath,
                null,
                cancellationToken);

            return backupPath;
        }
        catch (Exception ex)
        {
            sw.Stop();

            await RecordBackupAsync(
                connectionString,
                Path.GetFileName(backupPath),
                databaseName,
                0,
                "Failed",
                migrationId,
                backupPath,
                ex.Message,
                cancellationToken);

            throw;
        }
    }

    public async Task<IReadOnlyList<BackupRecord>> GetBackupRecordsAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(connectionString, cancellationToken);

        var records = new List<BackupRecord>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(SelectRecordSql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            records.Add(new BackupRecord
            {
                Id = reader.GetInt64(0),
                BackupName = reader.GetString(1),
                DatabaseName = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3),
                SizeBytes = reader.GetInt64(4),
                Status = reader.GetString(5),
                MigrationId = reader.IsDBNull(6) ? null : reader.GetString(6),
                Location = reader.GetString(7),
                ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return records;
    }

    private static async Task RecordBackupAsync(
        string connectionString,
        string backupName,
        string databaseName,
        long sizeBytes,
        string status,
        string? migrationId,
        string location,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await EnsureTableAsync(connectionString, cancellationToken);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand(RecordBackupSql, connection);

            cmd.Parameters.AddWithValue("@BackupName", backupName);
            cmd.Parameters.AddWithValue("@DatabaseName", databaseName);
            cmd.Parameters.AddWithValue("@SizeBytes", sizeBytes);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@MigrationId", (object?)migrationId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Location", location);
            cmd.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch
        {
            // Recording the backup into history must never mask the backup result.
        }
    }

    private static long GetBackupFileSize(string backupPath, string connectionString, CancellationToken cancellationToken)
    {
        long fileSize = 0;

        try
        {
            using var fs = new FileStream(backupPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            fileSize = fs.Length;
        }
        catch (IOException)
        {
            // Query msdb as a fallback when the backup resides on an SQL Server-relative path.
            fileSize = GetBackupSizeFromServer(connectionString, backupPath, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            fileSize = GetBackupSizeFromServer(connectionString, backupPath, cancellationToken);
        }

        return fileSize;
    }

    private static long GetBackupSizeFromServer(string connectionString, string backupPath, CancellationToken cancellationToken)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            using var cmd = new SqlCommand(
                "SELECT TOP (1) backup_size / 1024 FROM msdb.dbo.backupmediafamily bf " +
                "JOIN msdb.dbo.backupdevice bd ON bf.media_set_id = bd.media_set_id " +
                "WHERE bd.physical_device_name = @Path ORDER BY bf.media_set_id DESC",
                connection);
            cmd.Parameters.AddWithValue("@Path", backupPath);

            object? result = cmd.ExecuteScalar();
            return (result is long l && l > 0) ? l : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static string ResolveBackupPath(string databaseName)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Migrator",
            "Backups");

        string fileName = $"{SanitizeFileName(databaseName)}_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
        return Path.Combine(directory, fileName);
    }

    private static bool IsLocalServer(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        string dataSource = builder.DataSource?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return true;
        }

        string host = dataSource.Split(',')[0].Trim().Trim('[', ']');
        int slashIndex = host.IndexOf('\\');
        if (slashIndex >= 0)
        {
            host = host[..slashIndex];
        }

        return host.Equals(".", StringComparison.OrdinalIgnoreCase)
            || host.Equals("(local)", StringComparison.OrdinalIgnoreCase)
            || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase)
            || host.StartsWith("(localdb)", StringComparison.OrdinalIgnoreCase)
            || host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }

    private static async Task EnsureTableAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(EnsureTableSql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}