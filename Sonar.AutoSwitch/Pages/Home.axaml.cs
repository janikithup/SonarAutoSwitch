using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Sonar.AutoSwitch.Services;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Pages;

public partial class Home : UserControl
{
    private readonly StackPanel _activeConfigPanel;
    private readonly TextBox _searchBox;
    private readonly ToggleButton _searchToggle;
    private HomeViewModel? _hookedVm;

    public Home()
    {
        InitializeComponent();
        DataContext = HomeViewModel.LoadHomeViewModel();
        _activeConfigPanel = this.FindControl<StackPanel>("ActiveConfigPanel")!;
        _searchBox = this.FindControl<TextBox>("SearchBox")!;
        _searchToggle = this.FindControl<ToggleButton>("SearchToggle")!;

        // Follow the active DataContext so the title tracks switches (and so tests can inject a VM).
        DataContextChanged += (_, _) => HookViewModel();
        HookViewModel();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SyncWindowTitle();
    }

    private void HookViewModel()
    {
        if (_hookedVm != null) _hookedVm.PropertyChanged -= OnViewModelChanged;
        _hookedVm = DataContext as HomeViewModel;
        if (_hookedVm != null) _hookedVm.PropertyChanged += OnViewModelChanged;
        SyncWindowTitle();
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.ActiveProfile))
            SyncWindowTitle();
    }

    private void SyncWindowTitle()
    {
        if (TopLevel.GetTopLevel(this) is not Window w) return;
        var name = (DataContext as HomeViewModel)?.ActiveProfile?.Name;
        w.Title = string.IsNullOrEmpty(name) ? "Sonar Auto Switch" : $"Sonar Auto Switch — {name}";
    }

    private void OpenSettings_Click(object? sender, RoutedEventArgs e) =>
        ((MainWindow)TopLevel.GetTopLevel(this)!).ShowSettings();

    private void SearchToggle_Click(object? sender, RoutedEventArgs e)
    {
        var searching = _searchToggle.IsChecked == true;
        _activeConfigPanel.IsVisible = !searching;
        _searchBox.IsVisible = searching;
        if (searching)
            _searchBox.Focus();
        else if (DataContext is HomeViewModel vm)
            vm.SearchText = "";
    }

    private async void BrowseExe_Click(object? sender, RoutedEventArgs e)
    {
        var vm = ((Control)sender!).DataContext as AutoSwitchProfileViewModel;
        if (vm is null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select game executable",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Executable") { Patterns = ["*.exe"] }]
        });

        if (files is { Count: > 0 })
            vm.ExeName = Path.GetFileNameWithoutExtension(files[0].Name);
    }

    private void AddProfile_Click(object? sender, RoutedEventArgs e)
    {
        StateManager.Instance.GetOrLoadState<HomeViewModel>().AddAutoSwitchProfile();
        Dispatcher.UIThread.Post(() =>
        {
            this.GetVisualDescendants()
                .OfType<Expander>()
                .FirstOrDefault(exp => exp.IsExpanded)
                ?.BringIntoView();
        }, DispatcherPriority.Background);
    }
}
