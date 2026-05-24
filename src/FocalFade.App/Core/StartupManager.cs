using FocalFade.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace FocalFade.Core;

public sealed class StartupManager : IStartupManager
{
    private readonly ILogger<StartupManager> _logger;
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "FocalFade";

    public StartupManager(ILogger<StartupManager> logger)
    {
        _logger = logger;
    }

    public bool IsRegisteredForStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check startup registration");
            return false;
        }
    }

    public bool SetStartupRegistration(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null)
            {
                _logger.LogError("Could not open registry key for writing");
                return false;
            }

            if (enable)
            {
                var exePath = Environment.ProcessPath ?? "";
                key.SetValue(AppName, $"\"{exePath}\"", RegistryValueKind.String);
                _logger.LogInformation("Registered for startup: {Path}", exePath);
            }
            else
            {
                key.DeleteValue(AppName, false);
                _logger.LogInformation("Unregistered from startup");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set startup registration");
            return false;
        }
    }
}
