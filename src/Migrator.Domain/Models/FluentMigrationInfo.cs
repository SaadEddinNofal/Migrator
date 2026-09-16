using Migrator.Domain.Enums;

namespace Migrator.Domain.Models;

public sealed class FluentMigrationInfo
{
    public string AssemblyPath { get; init; } = string.Empty;

    public string AssemblyName { get; init; } = string.Empty;

    public long Version { get; init; }

    public string MigrationName { get; init; } = string.Empty;

    public MigrationType MigrationType { get; init; } = MigrationType.FluentMigrator;

    public string Namespace { get; init; } = string.Empty;
}