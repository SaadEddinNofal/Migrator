namespace Migrator.Domain.Enums;

public enum MigrationStatus
{
    Pending,
    Applied,
    Failed,
    Modified,
    Skipped
}