using Microsoft.Data.SqlClient;
using Migrator.Domain.Interfaces;

namespace Migrator.Infrastructure.SqlServer;

public sealed class SqlServerMigrationExecutor : IMigrationExecutor
{
    private readonly ISqlBatchParser _batchParser;

    public SqlServerMigrationExecutor(ISqlBatchParser batchParser)
    {
        _batchParser = batchParser;
    }

    public async Task ExecuteSqlAsync(string connectionString, string sqlContent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        IReadOnlyList<string> batches = _batchParser.ParseBatches(sqlContent);
        if (batches.Count == 0)
            return;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (string batch in batches)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var cmd = new SqlCommand(batch, connection, transaction)
                {
                    CommandTimeout = 0
                };
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}