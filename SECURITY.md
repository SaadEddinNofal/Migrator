# Security

This document describes the security considerations and measures implemented in the Migrator project.

## Credential Handling

### Never Logged

Database credentials and connection strings are never written to log files, console output, or any persistent storage in plaintext. Serilog loggers are configured to exclude sensitive fields:

- Connection strings are **never** included in Serilog output
- Passwords are **never** logged at any log level (Verbose through Fatal)
- Application settings containing credentials are excluded from diagnostic dumps

### Masked in UI

Password fields in the user interface are masked. Credentials entered by the user exist only in memory during the active session and are not displayed in plaintext in any UI element, tooltip, or status bar message.

## Connection String Security

- Connection strings are constructed at runtime from individual components (server, database, credentials) rather than stored as complete strings
- Windows Authentication (Integrated Security) is supported and preferred where possible, eliminating the need to handle passwords entirely
- When SQL Server Authentication is used, the password is held only in memory for the duration of the database session
- Connection strings are not serialized to disk, written to logs, or included in error reports

## Password Storage

- Passwords are **never persisted as plaintext** on disk
- Passwords are **never written** to configuration files, registry entries, or any other storage mechanism
- If the user chooses to save connection settings, the password field is excluded or stored only in the current user's in-memory session
- Application settings files (`appsettings.json`, user config) do not contain password values

## SQL Injection Prevention

- All migration SQL is user-authored and executed as-is — the tool is a migration runner, not an application with user-supplied queries
- Parameterized queries are used for all internal queries against the migration history table and metadata
- The `__MigratorHistory` table queries use parameterized commands, never string concatenation
- User inputs in the UI (server name, database name, file paths) are not interpolated into SQL strings used for internal operations

## Assembly Loading Safety

FluentMigrator DLL migration support involves loading external assemblies at runtime. The following safety measures are implemented:

### Loading Controls

- DLLs are loaded from a **dedicated, user-configured directory** — not from arbitrary paths
- Only files with a `.dll` extension in the configured DLL directory are considered for loading
- The tool does **not** recursively scan subdirectories for assemblies

### Assembly Isolation

- Assemblies are loaded into an **isolated context** where possible to prevent conflicts with the host application
- Only classes implementing known FluentMigrator base types (`Migration`, `AutoReversingMigration`, `ForwardOnlyMigration`) are discovered and executed
- Assembly metadata (type names, version) is logged for audit purposes

### Runtime Validation

- Assemblies are validated for expected types before execution
- Missing or corrupt DLLs are reported with clear error messages and do not crash the application
- Failed assembly loads are logged with file name and error details

### Recommendations

- Only load DLLs from **trusted sources** in the configured migration DLL directory
- Review DLL contents before placing them in the migration directory
- Restrict write access to the DLL directory to prevent unauthorized code placement

## Production Environment Warnings

The tool provides explicit warnings when connecting to databases identified as production environments:

- **Visual indicators** — Production connections are highlighted with warning colors and icons in the UI
- **Confirmation dialogs** — A mandatory confirmation step is presented before executing any migration against a production database
- **Connection string analysis** — Server and database name patterns are checked against common production naming conventions
- **Environment tagging** — Users can tag connections by environment (Development, Staging, Production) for clear identification

Production warnings cannot be dismissed silently — the user must actively acknowledge the production status before migration execution proceeds.

## Backup Security Considerations

- Backup files are written to a **user-specified path** with restricted default permissions
- Backup file names include timestamps to prevent accidental overwrites
- Backup paths are validated to ensure they point to writable, local, or trusted network locations
- Backup operations use the same database credentials as migration operations — no additional credential storage
- Backup file contents are standard SQL Server backup format and should be treated with the same sensitivity as the source database

## Logging Safety

### What Is Logged

- Migration file names and sequence numbers (not file contents)
- Execution timestamps and durations
- Success/failure status
- Error messages (with sensitive data redacted)
- Connection server name and database name (not credentials)
- Checksums (SHA-256 hashes, not source data)
- Application startup and shutdown events
- UI navigation events

### What Is Never Logged

- Passwords or connection string credentials
- SQL migration file contents (only file names and checksums are logged)
- User-entered sensitive configuration values
- Assembly binary contents

### Log File Security

- Log files are written to a configurable directory within the application's working area
- Log files use Serilog's rolling file sink with size and date-based rotation
- Log files are plain text and should be treated as sensitive — they contain database names, server names, and migration metadata
- Users should apply appropriate file system permissions to the log directory

## Settings Storage Security

- Application settings are stored in standard .NET configuration files within the user's application data directory
- Settings files do not contain passwords or credentials
- Connection metadata (server, database, authentication type) may be stored for convenience, but the password field is explicitly excluded
- Settings files respect standard Windows file system permissions based on the user profile
- Portable/deployment-mode configurations are stored alongside the executable and should be secured with appropriate directory permissions

## Summary of Security Properties

| Property                        | Status |
| ------------------------------- | ------ |
| Credentials never logged       | ✅     |
| Passwords masked in UI         | ✅     |
| No plaintext password storage  | ✅     |
| Parameterized internal queries | ✅     |
| Isolated DLL loading           | ✅     |
| Production confirmation gates  | ✅     |
| Log content redaction          | ✅     |
| Settings exclude passwords     | ✅     |
| Backup path validation         | ✅     |
| Checksum integrity verification| ✅     |
