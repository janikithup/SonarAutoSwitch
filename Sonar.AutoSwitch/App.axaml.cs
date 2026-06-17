using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Sonar.AutoSwitch.Services;
using Sonar.AutoSwitch.Services.Win32;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch;

public class App : Application
{
    public override void Initialize()
    {
        DataContext = new AppViewModel();
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var args = (ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Args ?? [];
        // --demo: seed placeholder state before anything reads it, for screenshot capture.
        var demo = args.Contains("--demo");
        if (demo)
        {
            // Point config reads at the demo presets so auto-match fires and dropdowns show
            // demo configs (not the user's real Sonar configs). Must precede DemoData build.
            SteelSeriesSonarService.Instance.ConfigQuery = () => DemoData.Configs;
            SteelSeriesSonarService.Instance.RefreshGamingConfigurations();
            StateManager.Instance.SeedReadOnly(DemoData.HomeViewModel());
        }

        var firstLoad = !StateManager.Instance.CheckStateExists<SettingsViewModel>();
        var settingsViewModel = StateManager.Instance.GetOrLoadState<SettingsViewModel>();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            // ponytail: Avalonia auto-shows desktop.MainWindow in Start(). Only create it when
            // it should be visible; Open() creates it lazily via MainWindow ??= new MainWindow().
            if (firstLoad || demo || args.Contains("--show"))
            {
                var mainWindow = new MainWindow();
                // Demo: grow to fit all cards so the screenshot has no scrollbar / cut-off.
                // Clear the fixed Height (NaN = auto) so SizeToContent can take over.
                if (demo)
                {
                    mainWindow.Height = double.NaN;
                    mainWindow.SizeToContent = SizeToContent.Height;
                }
                desktop.MainWindow = mainWindow;
                desktop.MainWindow.Show();
            }
            if (firstLoad && !demo)
                StateManager.Instance.SaveStateNow<SettingsViewModel>();
        }

        var homeViewModel = StateManager.Instance.GetOrLoadState<HomeViewModel>();
        var appVm = (AppViewModel)DataContext!;
        appVm.WireTooltip(homeViewModel, settingsViewModel);

        // Demo runs as an isolated, read-only window — never drive real Sonar switching.
        if (settingsViewModel.Enabled && !demo)
            AutoSwitchService.Instance.ToggleEnabled(settingsViewModel.Enabled);
        if (firstLoad && settingsViewModel.StartAtStartup)
            StartupService.RegisterInStartup(true);

        _ = AutoSwitchProfilesDatabase.Instance.LoadDatabaseAsync();
        base.OnFrameworkInitializationCompleted();
    }
}