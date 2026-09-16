using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Migrator.Domain.Interfaces;

namespace Migrator.Infrastructure.Backup;

public sealed class SqlServerBackupService : IBackupService
{
    public async Task<string> CreateBackupAsync(string connectionString, string databaseName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        string backupPath = ResolveBackupPath(databaseName);

        string? directory = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string quotedDatabaseName = "[" + databaseName.Replace("]", "]]") + "]";

        const string commandText = """
            BACKUP DATABASE @DatabaseName
            TO DISK = @BackupPath
            WITH INIT, COMPRESSION, STATS = 0;
            """;

        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var cmd = new SqlCommand(commandText, connection);
            cmd.Parameters.AddWithValue("@DatabaseName", quotedDatabaseName);
            cmd.Parameters.AddWithValue("@BackupPath", backupPath);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            sw.Stop();
        }
        catch
        {
            sw.Stop();
            throw;
        }

        return backupPath;
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

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
        return new string(value.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
    }
}