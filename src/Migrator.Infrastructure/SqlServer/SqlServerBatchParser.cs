using Migrator.Domain.Interfaces;

namespace Migrator.Infrastructure.SqlServer;

public sealed class SqlServerBatchParser : ISqlBatchParser
{
    public IReadOnlyList<string> ParseBatches(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return [];

        var batches = new List<string>();
        var current = new System.Text.StringBuilder();
        int i = 0;
        int length = sql.Length;

        while (i < length)
        {
            char c = sql[i];

            // Check for line comment
            if (c == '-' && i + 1 < length && sql[i + 1] == '-')
            {
                // Skip until end of line
                while (i < length && sql[i] != '\n')
                    current.Append(sql[i++]);
                if (i < length) current.Append(sql[i++]);
                continue;
            }

            // Check for block comment
            if (c == '/' && i + 1 < length && sql[i + 1] == '*')
            {
                current.Append(sql[i++]); // /
                current.Append(sql[i++]); // *
                while (i < length)
                {
                    if (sql[i] == '*' && i + 1 < length && sql[i + 1] == '/')
                    {
                        current.Append(sql[i++]); // *
                        current.Append(sql[i++]); // /
                        break;
                    }
                    current.Append(sql[i++]);
                }
                continue;
            }

            // Check for string literal
            if (c == '\'')
            {
                current.Append(sql[i++]);
                while (i < length)
                {
                    current.Append(sql[i]);
                    if (sql[i] == '\'' && i + 1 < length && sql[i + 1] == '\'')
                    {
                        // Escaped quote
                        i += 2;
                        continue;
                    }
                    if (sql[i] == '\'')
                    {
                        i++;
                        break;
                    }
                    i++;
                }
                continue;
            }

            // Check for bracket-quoted identifier
            if (c == '[')
            {
                current.Append(sql[i++]);
                while (i < length && sql[i] != ']')
                    current.Append(sql[i++]);
                if (i < length) current.Append(sql[i++]); // ]
                continue;
            }

            // Check for GO batch separator
            if (c == 'G' || c == 'g')
            {
                // Check if it's a standalone GO
                if (IsGoSeparator(sql, i))
                {
                    var batch = current.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(batch))
                        batches.Add(batch);
                    current.Clear();

                    // Skip past GO
                    i += 2;

                    // Skip trailing whitespace/newlines after GO
                    while (i < length && char.IsWhiteSpace(sql[i]))
                        i++;
                    continue;
                }
            }

            current.Append(sql[i]);
            i++;
        }

        // Add final batch
        var lastBatch = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastBatch))
            batches.Add(lastBatch);

        return batches;
    }

    private static bool IsGoSeparator(string sql, int position)
    {
        if (position + 2 > sql.Length)
            return false;

        // Check the two characters are G and O (case-insensitive)
        if (char.ToUpperInvariant(sql[position]) != 'G')
            return false;
        if (char.ToUpperInvariant(sql[position + 1]) != 'O')
            return false;

        // Check that there's nothing alphanumeric before the GO
        if (position > 0)
        {
            char before = sql[position - 1];
            if (char.IsLetterOrDigit(before) || before == '_')
                return false;
        }

        // Check that there's nothing alphanumeric after the GO
        if (position + 2 < sql.Length)
        {
            char after = sql[position + 2];
            if (char.IsLetterOrDigit(after) || after == '_')
                return false;
        }

        return true;
    }
}