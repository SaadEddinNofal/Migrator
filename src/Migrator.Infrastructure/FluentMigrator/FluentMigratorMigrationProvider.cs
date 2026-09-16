using System.Reflection;
using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Migrator.Domain.Enums;
using Migrator.Domain.Interfaces;
using Migrator.Domain.Models;

namespace Migrator.Infrastructure.FluentMigrator;

public sealed class FluentMigratorMigrationProvider : IMigrationProvider
{
    public async Task<IReadOnlyList<FluentMigrationInfo>> DiscoverFluentMigrationsAsync(string assemblyPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"FluentMigrator assembly not found: {assemblyPath}", assemblyPath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            string assemblyName = assembly.GetName().Name ?? assembly.FullName ?? string.Empty;

            cancellationToken.ThrowIfCancellationRequested();

            var migrations = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsInterface && typeof(Migration).IsAssignableFrom(t))
                .Select(t => new FluentMigrationInfo
                {
                    AssemblyPath = assemblyPath,
                    AssemblyName = assemblyName,
                    Version = t.GetCustomAttribute<MigrationAttribute>()?.Version ?? 0,
                    MigrationName = t.Name,
                    MigrationType = MigrationType.FluentMigrator,
                    Namespace = t.Namespace ?? string.Empty
                })
                .Where(m => m.Version > 0)
                .OrderBy(m => m.Version)
                .ToList();

            return migrations;
        }
        catch (ReflectionTypeLoadException ex)
        {
            var loadErrors = ex.LoaderExceptions
                .Where(e => e is not null)
                .Select(e => e!.Message)
                .Distinct();

            throw new InvalidOperationException(
                $"One or more types in assembly '{assemblyPath}' could not be loaded: {string.Join("; ", loadErrors)}",
                ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new InvalidOperationException($"'{assemblyPath}' is not a valid .NET assembly.", ex);
        }
    }

    public async Task ExecuteFluentMigrationAsync(string assemblyPath, long version, string connectionString, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"FluentMigrator assembly not found: {assemblyPath}", assemblyPath);
        }

        var assembly = Assembly.LoadFrom(assemblyPath);

        bool hasMigrations = assembly
            .GetTypes()
            .Any(t => t.IsClass && !t.IsAbstract && !t.IsInterface && typeof(Migration).IsAssignableFrom(t) &&
                      t.GetCustomAttribute<MigrationAttribute>()?.Version == version);

        if (!hasMigrations)
        {
            throw new InvalidOperationException(
                $"No migration with version {version} was found in assembly '{assemblyPath}'.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        using var serviceProvider = BuildServiceProvider(connectionString, assembly);
        using var scope = serviceProvider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

        runner.MigrateUp(version);
    }

    private static ServiceProvider BuildServiceProvider(string connectionString, Assembly assembly)
    {
        return new ServiceCollection()
            .AddLogging(builder => builder.AddFluentMigratorConsole())
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddSqlServer()
                .WithGlobalConnectionString(connectionString)
                .WithMigrationsIn(assembly))
            .BuildServiceProvider(validateScopes: false);
    }
}