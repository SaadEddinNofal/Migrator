namespace Migrator.Application.Services;

using System.IO;
using System.Text.RegularExpressions;
using Migrator.Application.Interfaces;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;

public sealed partial class MigrationDiscoveryService : IMigrationDiscoveryService
{
    private readonly IChecksumService _checksumService;
    private readonly ISqlBatchParser _sqlBatchParser;
    private readonly IMigrationProvider _migrationProvider;

    public MigrationDiscoveryService(
        IChecksumService checksumService,
        ISqlBatchParser sqlBatchParser,
        IMigrationProvider migrationProvider)
    {
        _checksumService = checksumService;
        _sqlBatchParser = sqlBatchParser;
        _migrationProvider = migrationProvider;
    }

    public Task<IReadOnlyList<SqlMigrationInfo>> DiscoverSqlMigrationsAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Migration file not found: {filePath}", filePath);
        }

        if (!SqlFilePattern().IsMatch(Path.GetFileName(filePath)))
        {
            throw new FormatException($"Migration file must match the pattern 'NNN_Name.sql' where NNN is numeric. Invalid file: {Path.GetFileName(filePath)}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        string content = File.ReadAllText(filePath);

        cancellationToken.ThrowIfCancellationRequested();

        string checksum = _checksumService.ComputeChecksum(content);
        IReadOnlyList<string> batches = _sqlBatchParser.ParseBatches(content);

        SqlMigrationInfo migration = SqlMigrationInfo.FromFilePath(filePath, content, checksum, batches.Count);

        return Task.FromResult<IReadOnlyList<SqlMigrationInfo>>([migration]);
    }

    public Task<IReadOnlyList<FluentMigrationInfo>> DiscoverFluentMigrationsAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"FluentMigrator assembly not found: {assemblyPath}", assemblyPath);
        }

        return _migrationProvider.DiscoverFluentMigrationsAsync(assemblyPath, cancellationToken);
    }

    [GeneratedRegex(@"^\d+_.+\.sql$", RegexOptions.IgnoreCase)]
    private static partial Regex SqlFilePattern();
}