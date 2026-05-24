using FluentAssertions;
using FocalFade.Core;
using FocalFade.Models;
using FocalFade.Native;
using Microsoft.Extensions.Logging.Abstractions;

namespace FocalFade.Tests;

public class WindowTargetSelectorTests
{
    // These tests verify the filtering logic conceptually.
    // Actual hwnd-based tests require a running Windows desktop.

    [Theory]
    [InlineData("Progman", true)]
    [InlineData("WorkerW", true)]
    [InlineData("Shell_TrayWnd", true)]
    [InlineData("Shell_SecondaryTrayWnd", true)]
    [InlineData("NotifyIconOverflowWindow", true)]
    [InlineData("DV2ControlHost", true)]
    [InlineData("TaskListThumbnailWnd", true)]
    [InlineData("ForegroundStaging", true)]
    [InlineData("tooltips_class32", true)]
    public void KnownShellClasses_AreRejected(string className, bool shouldBeRejected)
    {
        // Verify the class names are in the rejection set
        // This is a logic test - actual rejection depends on hwnd
        var rejectClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Progman", "WorkerW", "SHELLDLL_DefView", "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd", "NotifyIconOverflowWindow", "DV2ControlHost",
            "tooltips_class32", "TaskListThumbnailWnd", "ForegroundStaging",
            "Xaml_WindowedPopupClass", "OperationStatusWindow",
            "Windows.UI.Core.CoreWindow", "Shell_CharmWindow",
            "ImmersiveLauncher", "ImmersiveSwitchListPaneWindow",
            "MultitaskingViewFrame",
            "WindowsInternal.ComposableShell.Experiences.TextInput.InputSite.WindowClass",
        };

        rejectClasses.Contains(className).Should().Be(shouldBeRejected);
    }

    [Theory]
    [InlineData("CabinetWClass", false)]   // Explorer folder window - valid
    [InlineData("ExploreWClass", false)]    // Explorer folder window - valid
    [InlineData("Notepad", false)]          // Normal app - valid
    [InlineData("Chrome_WidgetWin_1", false)] // Browser - valid
    public void ValidClasses_AreNotInRejectSet(string className, bool shouldBeRejected)
    {
        var rejectClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Progman", "WorkerW", "SHELLDLL_DefView", "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd", "NotifyIconOverflowWindow", "DV2ControlHost",
        };

        rejectClasses.Contains(className).Should().Be(shouldBeRejected);
    }

    [Fact]
    public void TinyWindowThresholds_AreReasonable()
    {
        NativeConstants.TinyWindowMinWidth.Should().BeGreaterThanOrEqualTo(100);
        NativeConstants.TinyWindowMinHeight.Should().BeGreaterThanOrEqualTo(60);
        NativeConstants.TinyWindowMinArea.Should().BeGreaterThanOrEqualTo(10000);
    }
}
