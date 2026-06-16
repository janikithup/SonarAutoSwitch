using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Sonar.AutoSwitch.Services;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Pages;

public partial class Settings : UserControl
{
    private static readonly HttpClient _updateClient = new();
    static Settings() => _updateClient.DefaultRequestHeaders.Add("User-Agent", "SonarAutoSwitch");

    private readonly TextBlock _updateStatus;
    private readonly StackPanel _resetConfirmPanel;

    public Settings()
    {
        InitializeComponent();
        DataContext = StateManager.Instance.GetOrLoadState<SettingsViewModel>();

        var v = Assembly.GetExecutingAssembly().GetName().Version;
        this.FindControl<TextBlock>("VersionLabel")!.Text =
            v is null ? "Sonar Auto Switch" : $"Sonar Auto Switch v{v.Major}.{v.Minor}.{v.Build}";

        // x:Name fields are not populated by reflection-based AvaloniaXamlLoader; cache via FindControl.
        _updateStatus    = this.FindControl<TextBlock>("UpdateStatus")!;
        _resetConfirmPanel = this.FindControl<StackPanel>("ResetConfirmPanel")!;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OpenLog_Click(object? sender, RoutedEventArgs e)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sonar.AutoSwitch", "debug.log");
        if (File.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        else
            _updateStatus.Text = "No log file yet.";
    }

    private void OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sonar.AutoSwitch");
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private async void CheckUpdates_Click(object? sender, RoutedEventArgs e)
    {
        _updateStatus.Text = "Checking...";
        try
        {
            var json = await _updateClient.GetStringAsync(
                "https://api.github.com/repos/janikithup/SonarAutoSwitch-Continued/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            var current = v is null ? "" : $"v{v.Major}.{v.Minor}.{v.Build}";
            _updateStatus.Text = tag == current ? "Up to date." : $"{tag} available.";
        }
        catch (Exception ex)
        {
            Services.AutoSwitchService.Log($"CheckUpdates failed: {ex.GetType().Name}: {ex.Message}");
            _updateStatus.Text = "Could not check.";
        }
    }

    private async void CopyVersion_Click(object? sender, RoutedEventArgs e)
    {
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        var text = v is null ? "unknown" : $"v{v.Major}.{v.Minor}.{v.Build}";
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(text);
    }

    private void Reset_Click(object? sender, RoutedEventArgs e) =>
        _resetConfirmPanel.IsVisible = true;

    private void ResetCancel_Click(object? sender, RoutedEventArgs e) =>
        _resetConfirmPanel.IsVisible = false;

    private void ResetConfirm_Click(object? sender, RoutedEventArgs e)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sonar.AutoSwitch");
        foreach (var file in new[] { "HomeViewModel.json", "SettingsViewModel.json" })
        {
            var path = Path.Combine(folder, file);
            if (File.Exists(path)) File.Delete(path);
        }
        if (Environment.ProcessPath is { } exe)
            Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        Environment.Exit(0);
    }
}
