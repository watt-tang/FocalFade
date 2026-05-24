namespace FocalFade.Services;

public interface IDiagnosticsService
{
    string GetVersion();
    string GetSettingsFolderPath();
    string GetLogFolderPath();
    void OpenSettingsFolder();
    void OpenLogFolder();
}
