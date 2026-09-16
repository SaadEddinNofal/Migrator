namespace Migrator.Application.Interfaces;

using Migrator.Application.DTOs;
using Migrator.Domain.Models;

public interface IMigrationWorkflowService
{
    Task<MigrationPreviewInfo> GetPreviewInfoAsync(SqlMigrationInfo migration, string connectionString, CancellationToken cancellationToken = default);
    Task<MigrationPreviewInfo> GetPreviewInfoAsync(FluentMigrationInfo migration, string connectionString, CancellationToken cancellationToken = default);
    Task<DashboardData> GetDashboardDataAsync(string connectionString, IReadOnlyList<SqlMigrationInfo> migrations, CancellationToken cancellationToken = default);
}