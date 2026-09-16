# Migrator v1.0.0

A professional Windows desktop database migration tool for SQL Server, built with **.NET 10** and Windows Forms.

**Features**
- Run migrations from `.sql` files or compiled **FluentMigrator** assemblies (auto-discovered, executed in version order)
- **Validation & Preview** — safety-check a migration against `__MigratorHistory` before execution, detect checksum mismatches, and preview its status
- **Visual Dashboard** — Applied / Pending / Failed / Modified counts at a glance
- **Automatic Backups** before execution (local SQL Server instances; safely skipped for remote/hosted servers)
- **Full History Tracking** — every run recorded in `__MigratorHistory` with timestamp, duration, and checksum
- **Dedicated screens** for History, Logs, Backups, Settings, and About, with Dark/Light themes
- **Serilog file logging** and startup/shutdown tracking
- **Fixes included** — re-execution of previously failed migrations; backup fixes (`STATS` range, bracketed database names, remote/hosted servers)

**Attachment:** `Migrator.exe` — self-contained single-file build (win-x64), no .NET runtime install required.