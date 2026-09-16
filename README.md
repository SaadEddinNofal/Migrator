# Migrator

A professional Windows desktop database migration tool built with .NET 10 and Windows Forms. Migrator provides a clean, visual interface for executing SQL scripts and FluentMigrator DLL-based migrations against SQL Server databases, with full integrity verification, backup support, and migration history tracking.

## Features

- **SQL Migration Execution** — Run `.sql` migration files with automatic batch parsing (handles `GO` statements in strings and comments correctly)
- **FluentMigrator DLL Support** — Load and execute FluentMigrator migration assemblies at runtime
- **SQL Server Batch Parsing** — Intelligent splitting of SQL scripts that correctly respects `GO` delimiters inside strings, block comments, and line comments
- **SHA-256 Checksum Integrity** — Every migration file is checksummed before execution; history records the checksum to detect tampering
- **Migration History** — Tracks all executed migrations in a `__MigratorHistory` table inside the target database
- **Database Backups** — Optional SQL Server backup before migration execution
- **Dark / Light Theme** — Toggle between dark and light UI themes
- **Dashboard** — Overview with statistics on pending migrations, completed migrations, and database status
- **Validation & Preview** — Validate migration files, preview changes, and confirm before execution
- **Production Warnings** — Visual warnings when connecting to production environments
- **Serilog Logging** — Structured file logging with an in-app log viewer
- **Self-Contained Publishing** — Single-file executable for Windows x64 with no runtime dependencies

## Technology Stack

| Component             | Technology                                          |
| --------------------- | --------------------------------------------------- |
| Runtime               | .NET 10 LTS                                         |
| Language              | C#                                                  |
| UI Framework          | Windows Forms + [ReaLTaiizor](https://github.com/niktogo/ReaLTaiizor) |
| SQL Server Access     | Microsoft.Data.SqlClient                            |
| Migration Framework   | FluentMigrator (DLL-based migrations)               |
| Logging               | Serilog (file sink)                                 |
| Integrity             | SHA-256 checksums                                   |
| Architecture          | Clean Architecture (Domain → Application → Infrastructure → UI) |

## Architecture

The solution follows Clean Architecture with four layers:

```
┌─────────────────────────────────────────────────┐
│                  Migrator.UI                     │
│        Windows Forms · ReaLTaiizor              │
├─────────────────────────────────────────────────┤
│              Migrator.Application                │
│       Services · DTOs · Interfaces              │
├─────────────────────────────────────────────────┤
│            Migrator.Infrastructure               │
│   SqlServer · FluentMigrator · Backup ·         │
│   History · Checksum · Logging · Config         │
├─────────────────────────────────────────────────┤
│              Migrator.Domain                     │
│      Entities · Enums · Interfaces ·            │
│         Models · ValueObjects                   │
└─────────────────────────────────────────────────┘
```

Dependency direction: **UI → Application → Infrastructure → Domain**. The Domain layer has no external dependencies.

## Supported Migration Types

### SQL File Migrations

Place `.sql` files in a configured migrations directory. Files must follow the naming convention:

```
{NNN}_{Name}.sql
```

Where `NNN` is a zero-padded sequence number (e.g., `001_CreateUsers.sql`, `002_AddEmailColumn.sql`).

Migrations are executed in ascending order by sequence number. Each file is:
1. Validated (syntax check, checksum computed)
2. Previewed for user confirmation
3. Split into SQL batches using intelligent `GO` parsing
4. Executed against the target database
5. Recorded in `__MigratorHistory` with its SHA-256 checksum

### FluentMigrator DLL Migrations

Drop a compiled `.dll` containing FluentMigrator migration classes into the configured DLL directory. The tool loads the assembly at runtime, discovers all classes inheriting from `Migration` or `AutoReversingMigration`, and executes them in version order.

## Building

```bash
dotnet build
```

## Publishing

To create a self-contained single-file executable for Windows x64:

```bash
dotnet publish src/Migrator.UI/Migrator.UI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output will be a single `.exe` file in the publish directory with no additional runtime dependencies required on the target machine.

## How It Works

### Migration Execution Flow

1. **Scan** — The configured migrations directory is scanned for valid migration files
2. **Validate** — Each file is validated: naming convention, readability, and checksum computation
3. **History Check** — The tool queries `__MigratorHistory` to determine which migrations have already been executed
4. **Preview** — Pending (unexecuted) migrations are displayed for review
5. **Confirm** — The user confirms execution (with production environment warnings if applicable)
6. **Execute** — Migrations are executed in sequence order
7. **Record** — Each completed migration is recorded in `__MigratorHistory` with its checksum, timestamp, and execution metadata
8. **Backup** (optional) — A SQL Server backup can be performed before or after execution

### Migration History

The `__MigratorHistory` table is automatically created in the target database (if it does not exist) with the following schema:

| Column        | Description                                      |
| ------------- | ------------------------------------------------ |
| `Id`          | Auto-incrementing primary key                    |
| `Version`     | The migration version/sequence number            |
| `Name`        | Human-readable migration name                    |
| `Checksum`    | SHA-256 hash of the migration file content       |
| `AppliedAt`   | UTC timestamp when the migration was applied     |
| `ExecutionTimeMs` | Duration of execution in milliseconds       |
| `Success`     | Whether the migration completed successfully     |
| `ErrorMessage`| Error details if the migration failed            |

This table prevents re-execution of already-applied migrations and provides an audit trail.

### Checksum Verification

Before execution, each migration file is hashed using SHA-256. The resulting hex-encoded hash is stored in the history table. On subsequent runs, if a migration file's checksum does not match the recorded checksum, the tool flags a warning indicating the file may have been modified since its last execution.

### Backup

When enabled, the tool can perform a SQL Server backup using `BACKUP DATABASE` before migration execution. Backup options include:

- **Destination path** — Configurable file path for the backup file
- **Backup naming** — Automatic timestamp-based naming
- **Pre-migration backup** — Executed before any migrations run
- **Backup verification** — Optional integrity check after backup

## Project Structure

```
Migrator.slnx
src/
├── Migrator.Domain/
│   ├── Entities/          # Core business entities
│   ├── Enums/             # Enumeration types
│   ├── Interfaces/        # Domain-level abstractions
│   ├── Models/            # Domain models
│   └── ValueObjects/      # Immutable value types
├── Migrator.Application/
│   ├── Services/          # Application services and orchestration
│   ├── DTOs/              # Data transfer objects
│   └── Interfaces/        # Application-level abstractions
├── Migrator.Infrastructure/
│   ├── SqlServer/         # SQL Server data access and batch parsing
│   ├── FluentMigrator/    # FluentMigrator DLL loading and execution
│   ├── Backup/            # Database backup functionality
│   ├── History/           # Migration history tracking
│   ├── Checksum/          # SHA-256 checksum computation
│   ├── Logging/           # Serilog configuration and sinks
│   └── Config/            # Configuration and settings management
└── Migrator.UI/
    ├── Forms/             # Main application forms
    ├── Controls/          # Custom UI controls
    ├── Theme/             # Dark/Light theme management
    └── Program.cs         # Application entry point
```

## Version

1.0.0

## License

MIT
