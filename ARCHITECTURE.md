# Architecture

This document describes the architectural design of the Migrator project.

## Architecture Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                         Migrator.UI                              │
│                                                                  │
│   Forms ──── Controls ──── Theme ──── Program.cs                │
│                                                                  │
│   Responsibilities:                                              │
│   • User interaction and presentation logic                      │
│   • Theme management (Dark/Light)                                │
│   • Input validation at UI level                                 │
│   • Navigation and workflow orchestration                        │
└────────────────────────────┬─────────────────────────────────────┘
                             │ depends on
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                    Migrator.Application                          │
│                                                                  │
│   Services ──── DTOs ──── Interfaces                            │
│                                                                  │
│   Responsibilities:                                              │
│   • Business logic and use-case orchestration                    │
│   • Migration execution workflows                                │
│   • Validation, preview, and confirmation logic                  │
│   • DTOs for data transfer between layers                        │
└────────────────────────────┬─────────────────────────────────────┘
                             │ depends on
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                   Migrator.Infrastructure                        │
│                                                                  │
│   SqlServer ──┬─ FluentMigrator ──┬─ Backup                     │
│   History ────┤                   ├─ Checksum                    │
│   Logging ────┴─ Config ──────────┘                             │
│                                                                  │
│   Responsibilities:                                              │
│   • SQL Server connectivity and command execution                │
│   • SQL batch parsing (GO delimiter handling)                    │
│   • FluentMigrator assembly loading and execution                │
│   • Migration history persistence                                │
│   • SHA-256 checksum computation                                 │
│   • Database backup operations                                   │
│   • Serilog logging configuration                                │
│   • Application configuration and settings                       │
└────────────────────────────┬─────────────────────────────────────┘
                             │ depends on
                             ▼
┌──────────────────────────────────────────────────────────────────┐
│                       Migrator.Domain                            │
│                                                                  │
│   Entities ──┬─ Enums ──┬─ Interfaces                           │
│   Models ────┤          ├─ ValueObjects                          │
│              └──────────┘                                        │
│                                                                  │
│   Responsibilities:                                              │
│   • Core business entities and domain models                     │
│   • Domain-level interfaces (abstractions)                       │
│   • Enumerations and value objects                               │
│   • Zero external dependencies                                   │
└──────────────────────────────────────────────────────────────────┘
```

**Dependency direction:** UI → Application → Infrastructure → Domain

The Domain layer has no dependency on any other project or external package. Each layer only knows about the layer directly below it.

## Layer Descriptions

### Migrator.Domain

The innermost layer containing pure domain concepts with no external dependencies.

| Folder        | Purpose                                                        |
| ------------- | -------------------------------------------------------------- |
| `Entities`    | Core business entities such as migration records and history  |
| `Enums`       | Enumeration types (e.g., migration status, environment types) |
| `Interfaces`  | Abstractions that infrastructure implements (e.g., `IDatabaseProvider`) |
| `Models`      | Domain models representing migration metadata and results     |
| `ValueObjects`| Immutable value types (e.g., checksum, connection info)       |

This layer is a pure .NET class library with no NuGet dependencies, ensuring the domain model remains stable and framework-independent.

### Migrator.Application

Orchestrates business logic and coordinates between the UI and infrastructure layers.

| Folder      | Purpose                                                        |
| ----------- | -------------------------------------------------------------- |
| `Services`  | Application services that implement use cases (e.g., `MigrationOrchestrator`) |
| `DTOs`      | Data transfer objects for passing data between layers          |
| `Interfaces`| Application-level abstractions consumed by the UI layer        |

Application services depend on domain interfaces (not infrastructure implementations), following the Dependency Inversion Principle. Concrete implementations are injected via dependency injection.

### Migrator.Infrastructure

Contains all external implementations and integrations.

| Folder            | Purpose                                                      |
| ----------------- | ------------------------------------------------------------ |
| `SqlServer`       | SQL Server connection management, query execution, and intelligent SQL batch parsing with `GO` delimiter handling |
| `FluentMigrator`  | Runtime assembly loading, migration discovery, and FluentMigrator execution |
| `Backup`          | SQL Server database backup operations using `BACKUP DATABASE` |
| `History`         | `__MigratorHistory` table management and migration state tracking |
| `Checksum`        | SHA-256 checksum computation and verification for migration files |
| `Logging`         | Serilog pipeline configuration, file sink, and log rotation  |
| `Config`          | Application settings persistence and configuration management |

### Migrator.UI

The outermost layer — a Windows Forms application using ReaLTaiizor UI components.

| Folder     | Purpose                                                      |
| ---------- | ------------------------------------------------------------ |
| `Forms`    | Main application windows (dashboard, migration runner, log viewer, settings) |
| `Controls` | Custom ReaLTaiizor-based UI controls                        |
| `Theme`    | Dark/Light theme switching logic and theme resource management |
| `Program.cs` | Application entry point, DI container setup, and startup  |

## Key Abstractions

### IDatabaseProvider

```csharp
// Migrator.Domain
public interface IDatabaseProvider
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
    Task ExecuteSqlAsync(string sql, CancellationToken ct = default);
    Task<List<T>> QueryAsync<T>(string sql, CancellationToken ct = default);
    Task<bool> TableExistsAsync(string tableName, CancellationToken ct = default);
}
```

Abstracts database connectivity. Infrastructure provides the SQL Server implementation. This interface allows future support for other database engines.

### IMigrationProvider

```csharp
// Migrator.Domain
public interface IMigrationProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<MigrationInfo>> GetPendingMigrationsAsync(
        DatabaseConnectionInfo connection, CancellationToken ct = default);
    Task<MigrationResult> ExecuteMigrationAsync(
        MigrationInfo migration, DatabaseConnectionInfo connection, CancellationToken ct = default);
}
```

Defines the contract for migration providers. Two implementations exist:
- `SqlFileMigrationProvider` — Executes `.sql` files
- `FluentMigrationProvider` — Executes FluentMigrator DLL assemblies

### IMigrationExecutor

```csharp
// Migrator.Application
public interface IMigrationExecutor
{
    Task<ExecutionResult> RunAsync(
        IReadOnlyList<PendingMigration> migrations,
        ExecutionOptions options,
        CancellationToken ct = default);
}
```

Orchestrates the full migration execution pipeline: validate → execute → record history → handle errors.

### IMigrationHistory

```csharp
// Migrator.Domain
public interface IMigrationHistory
{
    Task EnsureTableExistsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<HistoryRecord>> GetAppliedMigrationsAsync(CancellationToken ct = default);
    Task RecordAsync(HistoryRecord record, CancellationToken ct = default);
    Task<bool> IsAppliedAsync(string version, CancellationToken ct = default);
}
```

Manages the `__MigratorHistory` table for tracking which migrations have been applied.

### IChecksumService

```csharp
// Migrator.Domain
public interface IChecksumService
{
    string ComputeSha256(byte[] data);
    string ComputeSha256(string filePath);
    bool Verify(string filePath, string expectedChecksum);
}
```

Provides SHA-256 checksum computation and verification for migration file integrity.

### IBatchParser

```csharp
// Migrator.Domain
public interface IBatchParser
{
    IReadOnlyList<string> SplitBatches(string sql);
}
```

Parses SQL scripts into individual batches by correctly handling `GO` delimiters within string literals, block comments, and line comments.

## Design Principles

### SOLID

- **Single Responsibility** — Each class has one reason to change. Providers handle migration discovery, executors handle orchestration, history handles persistence.
- **Open/Closed** — New migration providers (e.g., PostgreSQL, MySQL) can be added by implementing `IMigrationProvider` without modifying existing code.
- **Liskov Substitution** — All `IMigrationProvider` implementations are interchangeable.
- **Interface Segregation** — Domain interfaces are small and focused (e.g., `IChecksumService` is separate from `IMigrationHistory`).
- **Dependency Inversion** — High-level modules depend on abstractions in the Domain layer, not on Infrastructure implementations.

### Clean Architecture

- The **Domain** layer is the core with zero dependencies
- The **Application** layer orchestrates business logic using domain interfaces
- The **Infrastructure** layer provides concrete implementations
- The **UI** layer is the entry point and depends on Application services

### Dependency Injection

All dependencies are wired through DI at application startup in `Program.cs`. Infrastructure services are registered against their domain/application interfaces, enabling testability and replaceability.

## Data Flow

```
User clicks "Run Migrations"
        │
        ▼
   Migrator.UI (Form handler)
        │
        ├─ Validates UI inputs
        ├─ Displays confirmation dialog (production warning if applicable)
        │
        ▼
   Migrator.Application (MigrationOrchestrator)
        │
        ├─ Calls IMigrationProvider.GetPendingMigrationsAsync()
        │       │
        │       ▼
        │   Migrator.Infrastructure (SqlFileMigrationProvider)
        │       ├─ Scans directory for *.sql files
        │       ├─ Parses filenames (NNN_Name.sql)
        │       ├─ Calls IChecksumService.ComputeSha256()
        │       ├─ Calls IMigrationHistory.GetAppliedMigrationsAsync()
        │       └─ Returns list of pending migrations
        │
        ├─ User confirms execution
        │
        ├─ For each pending migration:
        │       │
        │       ├─ Calls IMigrationProvider.ExecuteMigrationAsync()
        │       │       │
        │       │       ├─ IBatchParser.SplitBatches(sql)
        │       │       ├─ IDatabaseProvider.ExecuteSqlAsync(batch)
        │       │       └─ Returns MigrationResult
        │       │
        │       ├─ Calls IMigrationHistory.RecordAsync()
        │       │       └─ Inserts into __MigratorHistory table
        │       │
        │       └─ Logs result via Serilog
        │
        └─ Returns ExecutionResult to UI
                │
                ▼
        UI updates dashboard with results
```

## Extension Points

### Adding a New Database Provider

To support a new database engine (e.g., PostgreSQL):

1. **Implement `IDatabaseProvider`** in `Migrator.Infrastructure`:
   ```csharp
   public class PostgresDatabaseProvider : IDatabaseProvider
   {
       public async Task<bool> TestConnectionAsync(CancellationToken ct = default) { ... }
       public async Task ExecuteSqlAsync(string sql, CancellationToken ct = default) { ... }
       public async Task<List<T>> QueryAsync<T>(string sql, CancellationToken ct = default) { ... }
       public async Task<bool> TableExistsAsync(string tableName, CancellationToken ct = default) { ... }
   }
   ```

2. **Register in DI** in `Program.cs`:
   ```csharp
   services.AddSingleton<IDatabaseProvider, PostgresDatabaseProvider>();
   ```

3. **Update connection UI** to support PostgreSQL connection parameters.

The application layer and domain layer remain unchanged.

### Adding a New Migration Provider

To support a different migration framework:

1. **Implement `IMigrationProvider`** in `Migrator.Infrastructure`
2. **Register in DI** in `Program.cs`
3. The `MigrationOrchestrator` will automatically discover and use the new provider

### Adding a New Backup Strategy

To support backup to cloud storage or different backup mechanisms:

1. **Define a new interface** in `Migrator.Domain` (e.g., `IBackupStrategy`)
2. **Implement** in `Migrator.Infrastructure`
3. **Register in DI**
4. **Add configuration** options for the new backup strategy

### Adding a New Theme

1. **Create theme resources** in `Migrator.UI/Theme/`
2. **Implement `IThemeProvider`** (or equivalent) with color definitions
3. **Register** with the theme manager
4. The UI controls will pick up theme changes at runtime
