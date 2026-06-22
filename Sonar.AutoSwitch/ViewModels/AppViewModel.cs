using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Sonar.AutoSwitch.Services;

namespace Sonar.AutoSwitch.ViewModels;

public class AppViewModel : ViewModelBase
{
    private string _trayTooltipText = "Sonar Auto Switch";

    public string TrayTooltipText
    {
        get => _trayTooltipText;
        private set => SetField(ref _trayTooltipText, value);
    }

    public void WireTooltip(HomeViewModel home, SettingsViewModel settings)
    {
        UpdateTooltip(home, settings);
        home.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is null or nameof(HomeViewModel.ActiveProfile))
                UpdateTooltip(home, settings);
        };
        // ponytail: override strips [CallerMemberName]; null = "all changed" in Avalonia.
        settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is null or nameof(SettingsViewModel.Enabled))
                UpdateTooltip(home, settings);
        };
    }

    private void UpdateTooltip(HomeViewModel home, SettingsViewModel settings)
    {
        if (!settings.Enabled)
        {
            TrayTooltipText = "Sonar Auto Switch — disabled";
            return;
        }
        var name = home.ActiveProfile?.Name;
        TrayTooltipText = string.IsNullOrEmpty(name)
            ? "Sonar Auto Switch"
            : $"Sonar Auto Switch — {name}";
    }

    public void Open()
    {
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if (lifetime.MainWindow is { IsVisible: true })
            {
                lifetime.MainWindow.Activate();
                return;
            }
            lifetime.MainWindow ??= App.EarlyWindow ?? new MainWindow();
            App.EarlyWindow = null;
            lifetime.MainWindow.Show();
        }
    }

    public void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }
}