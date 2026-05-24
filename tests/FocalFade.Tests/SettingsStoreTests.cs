using FluentAssertions;
using FocalFade.Core;
using FocalFade.Models;
using FocalFade.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FocalFade.Tests;

public class SettingsStoreTests
{
    private readonly SettingsStore _store;

    public SettingsStoreTests()
    {
        _store = new SettingsStore(NullLogger<SettingsStore>.Instance);
    }

    [Fact]
    public void DefaultSettings_HasCorrectValues()
    {
        var settings = new AppSettings();

        settings.SchemaVersion.Should().Be(2);
        settings.Enabled.Should().BeTrue();
        settings.StartEnabled.Should().BeTrue();
        settings.Opacity.Should().Be(0.45);
        settings.DimColor.Should().Be("#000000");
        settings.FocusMargin.Should().Be(8.0);
        settings.CornerRadius.Should().Be(8.0);
        settings.AnimationsEnabled.Should().BeTrue();
        settings.FadeDurationMs.Should().Be(120);
        settings.OverlayMode.Should().Be(OverlayMode.ActiveWindow);
        settings.PauseOnFullscreen.Should().BeTrue();
        settings.AppRules.Should().NotBeEmpty();
    }

    [Fact]
    public void DefaultHotkeys_AreConfigured()
    {
        var hotkeys = AppSettings.GetDefaultHotkeys();

        hotkeys.Should().ContainKey("ToggleEnabled");
        hotkeys.Should().ContainKey("IncreaseOpacity");
        hotkeys.Should().ContainKey("DecreaseOpacity");
        hotkeys.Should().ContainKey("PresentationMode");
        hotkeys.Should().ContainKey("TemporaryPeek");
    }

    [Fact]
    public void DefaultAppRules_ContainExpectedApps()
    {
        var rules = AppSettings.GetDefaultAppRules();

        rules.Should().Contain(r => r.ProcessName == "POWERPNT.EXE");
        rules.Should().Contain(r => r.ProcessName == "obs64.exe");
        rules.Should().Contain(r => r.ProcessName == "vlc.exe");
    }

    [Fact]
    public void Update_AppliesChangesCorrectly()
    {
        // Use a temp path to avoid affecting real settings
        var store = new TestableSettingsStore();

        store.Update(s => s with { Opacity = 0.70, Enabled = false });

        store.Settings.Opacity.Should().Be(0.70);
        store.Settings.Enabled.Should().BeFalse();
    }

    [Fact]
    public void ResetToDefaults_RestoresDefaults()
    {
        var store = new TestableSettingsStore();
        store.Update(s => s with { Opacity = 0.99 });
        store.Settings.Opacity.Should().Be(0.99);

        store.ResetToDefaults();
        store.Settings.Opacity.Should().Be(0.45);
    }

    // Helper class that doesn't write to disk
    private class TestableSettingsStore : ISettingsStore
    {
        private AppSettings _settings = new();
        public AppSettings Settings => _settings;
        public event EventHandler<AppSettings>? SettingsChanged;
        public string SettingsFilePath => string.Empty;
        public void Load() { }
        public void Save() { }
        public void Update(Func<AppSettings, AppSettings> modifier)
        {
            _settings = modifier(_settings);
            SettingsChanged?.Invoke(this, _settings);
        }
        public void ResetToDefaults()
        {
            _settings = new AppSettings();
            SettingsChanged?.Invoke(this, _settings);
        }
    }
}
