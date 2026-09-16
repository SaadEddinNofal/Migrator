using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Migrator.Application.Interfaces;
using Migrator.Application.Services;
using Migrator.Infrastructure.Extensions;
using Migrator.UI.Forms;
using Migrator.UI.Theme;
using Serilog;
using Application = System.Windows.Forms.Application;

namespace Migrator.UI;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        IHost host = Host.CreateDefaultBuilder()
            .UseSerilog((context, configuration) =>
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Migrator", "Logs", "migrator-.log");
                configuration
                    .MinimumLevel.Information()
                    .Enrich.FromLogContext()
                    .WriteTo.File(logPath, rollingInterval: RollingInterval.Day,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}");
            })
            .ConfigureServices((_, services) =>
            {
                services.AddMigratorInfrastructure();
                services.AddSingleton<IConnectionService, ConnectionService>();
                services.AddSingleton<IMigrationDiscoveryService, MigrationDiscoveryService>();
                services.AddSingleton<IMigrationValidationService, MigrationValidationService>();
                services.AddSingleton<IMigrationWorkflowService, MigrationWorkflowService>();
                services.AddSingleton<MigrationExecutorService>();
                services.AddSingleton<ThemeManager>();
                services.AddSingleton<MainForm>();
            })
            .Build();

        var settingsService = host.Services.GetRequiredService<Infrastructure.Configuration.UserSettingsService>();
        var themeManager = host.Services.GetRequiredService<ThemeManager>();
        themeManager.ApplyTheme(settingsService.Current.Theme);

        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        var mainForm = host.Services.GetRequiredService<MainForm>();
        System.Windows.Forms.Application.Run(mainForm);
    }
}