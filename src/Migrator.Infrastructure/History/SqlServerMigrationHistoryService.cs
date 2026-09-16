using Microsoft.Data.SqlClient;
using Migrator.Domain.Entities;
using Migrator.Domain.Enums;
using Migrator.Domain.Interfaces;

namespace Migrator.Infrastructure.History;

public sealed class SqlServerMigrationHistoryService : IMigrationHistoryService
{
    private const string EnsureTableSql = """
        IF OBJECT_ID(N'dbo.__MigratorHistory', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.__MigratorHistory (
                Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MigratorHistory PRIMARY KEY,
                MigrationId NVARCHAR(256) NOT NULL,
                MigrationName NVARCHAR(512) NOT NULL,
                MigrationType NVARCHAR(50) NOT NULL,
                Provider NVARCHAR(50) NOT NULL,
                Checksum NVARCHAR(128) NOT NULL,
                ExecutedAt DATETIME2 NOT NULL CONSTRAINT DF_MigratorHistory_ExecutedAt DEFAULT (SYSUTCDATETIME()),
                ExecutionDurationMs BIGINT NOT NULL,
                Status NVARCHAR(50) NOT NULL,
                AppliedBy NVARCHAR(128) NOT NULL,
                MachineName NVARCHAR(128) NOT NULL,
                DatabaseName NVARCHAR(128) NOT NULL,
                ServerName NVARCHAR(256) NOT NULL,
                ErrorMessage NVARCHAR(MAX) NULL
            );

            CREATE UNIQUE INDEX IX_MigratorHistory_MigrationId_Status ON dbo.__MigratorHistory (MigrationId, Status);
            CREATE INDEX IX_MigratorHistory_ExecutedAt ON dbo.__MigratorHistory (ExecutedAt);
        END
        """;

    private const string RecordStartSql = """
        IF EXISTS (SELECT 1 FROM dbo.__MigratorHistory WHERE MigrationId = @MigrationId)
        BEGIN
            UPDATE dbo.__MigratorHistory
            SET MigrationName = @MigrationName,
                MigrationType = @MigrationType,
                Provider = @Provider,
                Checksum = @Checksum,
                ExecutedAt = SYSUTCDATETIME(),
                ExecutionDurationMs = 0,
                Status = N'Pending',
                AppliedBy = @AppliedBy,
                MachineName = @MachineName,
                DatabaseName = @DatabaseName,
                ServerName = @ServerName,
                ErrorMessage = NULL
            WHERE MigrationId = @MigrationId;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.__MigratorHistory (
                MigrationId, MigrationName, MigrationType, Provider, Checksum, ExecutedAt,
                ExecutionDurationMs, Status, AppliedBy, MachineName, DatabaseName, ServerName, ErrorMessage)
            VALUES (
                @MigrationId, @MigrationName, @MigrationType, @Provider, @Checksum, SYSUTCDATETIME(),
                0, N'Pending', @AppliedBy, @MachineName, @DatabaseName, @ServerName, NULL);
        END
        """;

    private const string UpdateStatusSql = """
        UPDATE dbo.__MigratorHistory
        SET Status = @Status,
            ExecutionDurationMs = @ExecutionDurationMs,
            ExecutedAt = SYSUTCDATETIME(),
            ErrorMessage = @ErrorMessage
        WHERE MigrationId = @MigrationId;
        """;

    private const string FallbackInsertSql = """
        INSERT INTO dbo.__MigratorHistory (
            MigrationId, MigrationName, MigrationType, Provider, Checksum, ExecutedAt,
            ExecutionDurationMs, Status, AppliedBy, MachineName, DatabaseName, ServerName, ErrorMessage)
        VALUES (
            @MigrationId, N'', N'Unknown', @Provider, N'', SYSUTCDATETIME(),
            @ExecutionDurationMs, @Status, @AppliedBy, @MachineName, @DatabaseName, @ServerName, @ErrorMessage);
        """;

    public async Task<IReadOnlyList<MigrationHistoryEntry>> GetAllAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(connectionString, cancellationToken);

        const string sql = """
            SELECT Id, MigrationId, MigrationName, MigrationType, Provider, Checksum, ExecutedAt,
                   ExecutionDurationMs, Status, AppliedBy, MachineName, DatabaseName, ServerName, ErrorMessage
            FROM dbo.__MigratorHistory
            ORDER BY ExecutedAt DESC, Id DESC;
            """;

        var entries = new List<MigrationHistoryEntry>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(ReadEntry(reader));
        }

        return entries;
    }

    public async Task<MigrationHistoryEntry?> GetByMigrationIdAsync(string connectionString, string migrationId, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(connectionString, cancellationToken);

        const string sql = """
            SELECT TOP (1) Id, MigrationId, MigrationName, MigrationType, Provider, Checksum, ExecutedAt,
                   ExecutionDurationMs, Status, AppliedBy, MachineName, DatabaseName, ServerName, ErrorMessage
            FROM dbo.__MigratorHistory
            WHERE MigrationId = @MigrationId
            ORDER BY ExecutedAt DESC, Id DESC;
            """;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@MigrationId", migrationId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken) ? ReadEntry(reader) : null;
    }

    public async Task RecordStartAsync(
        string connectionString,
        string migrationId,
        string migrationName,
        MigrationType migrationType,
        string checksum,
        CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(connectionString, cancellationToken);

        var builder = new SqlConnectionStringBuilder(connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(RecordStartSql, connection);

        cmd.Parameters.AddWithValue("@MigrationId", migrationId);
        cmd.Parameters.AddWithValue("@MigrationName", migrationName);
        cmd.Parameters.AddWithValue("@MigrationType", migrationType.ToString());
        cmd.Parameters.AddWithValue("@Provider", DatabaseProviderType.SqlServer.ToString());
        cmd.Parameters.AddWithValue("@Checksum", checksum);
        cmd.Parameters.AddWithValue("@AppliedBy", Environment.UserName);
        cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName);
        cmd.Parameters.AddWithValue("@DatabaseName", builder.InitialCatalog ?? "Unknown");
        cmd.Parameters.AddWithValue("@ServerName", builder.DataSource ?? "Unknown");

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RecordSuccessAsync(string connectionString, string migrationId, long executionDurationMs, CancellationToken cancellationToken = default)
    {
        int updated = await UpdateStatusAsync(
            connectionString, migrationId, MigrationStatus.Applied, executionDurationMs, null, cancellationToken);

        if (updated == 0)
        {
            await InsertFallbackAsync(connectionString, migrationId, MigrationStatus.Applied, executionDurationMs, null, cancellationToken);
        }
    }

    public async Task RecordFailureAsync(string connectionString, string migrationId, string errorMessage, long executionDurationMs, CancellationToken cancellationToken = default)
    {
        int updated = await UpdateStatusAsync(
            connectionString, migrationId, MigrationStatus.Failed, executionDurationMs, errorMessage, cancellationToken);

        if (updated == 0)
        {
            await InsertFallbackAsync(connectionString, migrationId, MigrationStatus.Failed, executionDurationMs, errorMessage, cancellationToken);
        }
    }

    private static async Task<int> UpdateStatusAsync(
        string connectionString,
        string migrationId,
        MigrationStatus status,
        long executionDurationMs,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(UpdateStatusSql, connection);

        cmd.Parameters.AddWithValue("@MigrationId", migrationId);
        cmd.Parameters.AddWithValue("@Status", status.ToString());
        cmd.Parameters.AddWithValue("@ExecutionDurationMs", executionDurationMs);
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task InsertFallbackAsync(
        string connectionString,
        string migrationId,
        MigrationStatus status,
        long executionDurationMs,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(FallbackInsertSql, connection);

        cmd.Parameters.AddWithValue("@MigrationId", migrationId);
        cmd.Parameters.AddWithValue("@Provider", DatabaseProviderType.SqlServer.ToString());
        cmd.Parameters.AddWithValue("@Status", status.ToString());
        cmd.Parameters.AddWithValue("@ExecutionDurationMs", executionDurationMs);
        cmd.Parameters.AddWithValue("@AppliedBy", Environment.UserName);
        cmd.Parameters.AddWithValue("@MachineName", Environment.MachineName);
        cmd.Parameters.AddWithValue("@DatabaseName", builder.InitialCatalog ?? "Unknown");
        cmd.Parameters.AddWithValue("@ServerName", builder.DataSource ?? "Unknown");
        cmd.Parameters.AddWithValue("@ErrorMessage", (object?)errorMessage ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureTableAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(EnsureTableSql, connection);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static MigrationHistoryEntry ReadEntry(SqlDataReader reader)
    {
        static TEnum ParseEnum<TEnum>(string value) where TEnum : struct, Enum =>
            Enum.TryParse<TEnum>(value, out var parsed) ? parsed : default;

        return new MigrationHistoryEntry
        {
            Id = reader.GetInt64(0),
            MigrationId = reader.GetString(1),
            MigrationName = reader.GetString(2),
            MigrationType = ParseEnum<MigrationType>(reader.GetString(3)),
            Provider = ParseEnum<DatabaseProviderType>(reader.GetString(4)),
            Checksum = reader.GetString(5),
            ExecutedAt = reader.GetDateTime(6),
            ExecutionDurationMs = reader.GetInt64(7),
            Status = ParseEnum<MigrationStatus>(reader.GetString(8)),
            AppliedBy = reader.GetString(9),
            MachineName = reader.GetString(10),
            DatabaseName = reader.GetString(11),
            ServerName = reader.GetString(12),
            ErrorMessage = reader.IsDBNull(13) ? null : reader.GetString(13)
        };
    }
}