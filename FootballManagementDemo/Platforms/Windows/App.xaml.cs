using Microsoft.UI.Xaml;

namespace FootballManagementDemo.WinUI;

// Adjusted to inherit Application to avoid a base-class mismatch with the shared App
// partial when building multiple TFMs in CI or tooling environments. This keeps the
// Windows-specific code simple for compile-time verification. If you need the
// MauiWinUIApplication behavior for packaging/running on Windows, revert this and
// run the app on a Windows machine/emulator.
public partial class App : global::Microsoft.UI.Xaml.Application
{
    public App()
    {
        InitializeComponent();
    }
}
