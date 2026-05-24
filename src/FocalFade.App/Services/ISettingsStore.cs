using FocalFade.Models;

namespace FocalFade.Services;

public interface ISettingsStore
{
    AppSettings Settings { get; }
    event EventHandler<AppSettings>? SettingsChanged;
    void Load();
    void Save();
    void Update(Func<AppSettings, AppSettings> modifier);
    void ResetToDefaults();
    string SettingsFilePath { get; }
}
