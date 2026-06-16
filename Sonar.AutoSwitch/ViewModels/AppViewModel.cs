using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Sonar.AutoSwitch.Services;

namespace Sonar.AutoSwitch.ViewModels;

public class AppViewModel : ViewModelBase
{
    private string _trayTooltipText = "Sonar Auto Switch";
    private bool _showRunningInBackgroundNotice;
    private SettingsViewModel? _settings;

    public string TrayTooltipText
    {
        get => _trayTooltipText;
        private set => SetField(ref _trayTooltipText, value);
    }

    public bool ShowRunningInBackgroundNotice
    {
        get => _showRunningInBackgroundNotice;
        set => SetField(ref _showRunningInBackgroundNotice, value);
    }

    public void WireTooltip(HomeViewModel home, SettingsViewModel settings)
    {
        _settings = settings;
        UpdateTooltip(home, settings);
        home.AutoSwitchProfiles.CollectionChanged += (_, _) => UpdateTooltip(home, settings);
        // ponytail: override strips [CallerMemberName]; null = "all changed" in Avalonia.
        settings.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is null or nameof(SettingsViewModel.Enabled))
                UpdateTooltip(home, settings);
        };
    }

    public void DismissBackgroundNotice()
    {
        ShowRunningInBackgroundNotice = false;
        if (_settings is null) return;
        // ponytail: debounce could miss save on immediate exit; notice may reappear once — acceptable.
        _settings.HasShownTrayNotification = true;
        StateManager.Instance.SaveState<SettingsViewModel>();
    }

    private void UpdateTooltip(HomeViewModel home, SettingsViewModel settings)
    {
        int n = home.AutoSwitchProfiles.Count;
        TrayTooltipText = settings.Enabled
            ? $"Sonar Auto Switch — {n} profile{(n == 1 ? "" : "s")}"
            : "Sonar Auto Switch — disabled";
    }

    public void Open()
    {
        DismissBackgroundNotice();
        if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.MainWindow ??= new MainWindow();
            lifetime.MainWindow.Show();
        }
    }

    public void Exit()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
            lifetime.Shutdown();
    }
}