using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.IO;

namespace FocalFade.Core;

public sealed class DiagnosticsService : IDiagnosticsService
{
    private readonly ILogger<DiagnosticsService> _logger;

    public DiagnosticsService(ILogger<DiagnosticsService> logger)
    {
        _logger = logger;
    }

    public string GetVersion()
    {
        var version = typeof(DiagnosticsService).Assembly.GetName().Version;
        return version?.ToString() ?? "1.0.0";
    }

    public string GetSettingsFolderPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocalFade");
    }

    public string GetLogFolderPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FocalFade", "Logs");
    }

    public void OpenSettingsFolder()
    {
        var path = GetSettingsFolderPath();
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }

    public void OpenLogFolder()
    {
        var path = GetLogFolderPath();
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}
