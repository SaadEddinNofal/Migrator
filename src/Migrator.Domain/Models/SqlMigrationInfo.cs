using System.Text;

namespace Migrator.Domain.Models;

public sealed class SqlMigrationInfo
{
    private static readonly char[] FilenameSeparators = ['_', '.'];

    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public long FileSizeBytes { get; init; }

    public string Checksum { get; init; } = string.Empty;

    public string MigrationId { get; init; } = string.Empty;

    public string MigrationName { get; init; } = string.Empty;

    public int BatchCount { get; init; }

    public static SqlMigrationInfo FromFilePath(string filePath, string content, string checksum, int batchCount)
    {
        string fileName = Path.GetFileName(filePath);
        string migrationId = ExtractMigrationId(fileName);
        string migrationName = ExtractMigrationName(fileName);

        return new SqlMigrationInfo
        {
            FilePath = filePath,
            FileName = fileName,
            Content = content,
            FileSizeBytes = Encoding.UTF8.GetByteCount(content),
            Checksum = checksum,
            MigrationId = migrationId,
            MigrationName = migrationName,
            BatchCount = batchCount
        };
    }

    private static string ExtractMigrationId(string fileName)
    {
        int separatorIndex = fileName.IndexOfAny(FilenameSeparators);
        return separatorIndex > 0 ? fileName[..separatorIndex] : fileName;
    }

    private static string ExtractMigrationName(string fileName)
    {
        string withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        int separatorIndex = withoutExtension.IndexOfAny(FilenameSeparators);
        return separatorIndex > 0 ? withoutExtension[(separatorIndex + 1)..] : withoutExtension;
    }
}