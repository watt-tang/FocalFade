using FocalFade.Core;
using FocalFade.Tray;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace FocalFade;

public partial class App : Application
{
    private IHost? _host;
    private AppBootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Ensure log directory exists
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocalFade", "Logs");
        Directory.CreateDirectory(logDir);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddSingleton<Services.ISettingsStore, SettingsStore>();
                services.AddSingleton<Services.IMonitorManager, MonitorManager>();
                services.AddSingleton<Services.IActiveWindowTracker, ActiveWindowTracker>();
                services.AddSingleton<Services.IOverlayManager, OverlayManager>();
                services.AddSingleton<Services.IHotkeyManager, HotkeyManager>();
                services.AddSingleton<Services.IStartupManager, StartupManager>();
                services.AddSingleton<Services.IAppRuleManager, AppRuleManager>();
                services.AddSingleton<Services.IDiagnosticsService, DiagnosticsService>();
                services.AddSingleton<WindowInfoProvider>();
                services.AddSingleton<WindowTargetSelector>();
                services.AddSingleton<TrayService>();
                services.AddSingleton<TrayThemeService>();
                services.AddSingleton<SingleInstanceGuard>();
                services.AddSingleton<AppLifecycleService>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        _bootstrapper = new AppBootstrapper(_host);
        _bootstrapper.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _bootstrapper?.Dispose();
        _host?.Dispose();
        base.OnExit(e);
    }
}
