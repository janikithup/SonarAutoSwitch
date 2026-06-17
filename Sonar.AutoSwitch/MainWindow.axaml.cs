using Avalonia.Controls;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Sonar.AutoSwitch.Pages;
using Sonar.AutoSwitch.Services;
using Sonar.AutoSwitch.Services.Win32;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch;

public partial class MainWindow : Window
{
    private readonly ContentControl _pageHost;
    private readonly Home _homePage;
    private readonly Settings _settingsPage;
    private bool _trayBalloonShown;

    public MainWindow()
    {
        InitializeComponent();
        _pageHost = this.FindControl<ContentControl>("PageHost")!;
        _homePage = new Home();
        _settingsPage = new Settings();
        _pageHost.Content = _homePage;
        // Drop the cached Sonar config list on focus so configs added in Sonar GG
        // appear on the next card render without an app restart. ponytail: cache
        // invalidation only; live ComboBoxes refresh when their card next realizes.
        Activated += (_, _) => SteelSeriesSonarService.Instance.RefreshGamingConfigurations();
    }

    public void ShowSettings() => _pageHost.Content = _settingsPage;
    public void ShowHome() => _pageHost.Content = _homePage;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (!IsVisible) return;
        var settings = StateManager.Instance.GetOrLoadState<SettingsViewModel>();
        if (settings.CloseToTray)
        {
            if (!_trayBalloonShown)
            {
                _trayBalloonShown = true;
                TrayBalloon.Show("Sonar Auto Switch",
                    "Still running in the background. Right-click the tray icon to exit.");
            }
            Hide();
            e.Cancel = true;
        }
        else
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                    desktop.Shutdown();
            });
        }
    }
}
