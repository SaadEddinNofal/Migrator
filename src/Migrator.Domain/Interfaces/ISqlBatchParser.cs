namespace Migrator.Domain.Interfaces;

public interface ISqlBatchParser
{
    IReadOnlyList<string> ParseBatches(string sql);
}