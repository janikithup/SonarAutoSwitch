using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using Sonar.AutoSwitch.Services;

namespace Sonar.AutoSwitch.ViewModels;

public class HomeViewModel : ViewModelBase
{
    private ObservableCollection<AutoSwitchProfileViewModel> _autoSwitchProfiles =
        new() { new AutoSwitchProfileViewModel() };

    private SonarGamingConfiguration _defaultSonarGamingConfiguration = new(null, "unset");
    private SonarGamingConfiguration _activeProfile;

    public static IReadOnlyList<string> ProcessNames { get; } =
        Process.GetProcesses().Select(p => p.ProcessName).Distinct().OrderBy(x => x).ToList();

    public HomeViewModel()
    {
        foreach (var p in _autoSwitchProfiles)
            Subscribe(p);
        _autoSwitchProfiles.CollectionChanged += AutoSwitchProfilesOnCollectionChanged;
        if (_autoSwitchProfiles.FirstOrDefault() is { } first)
            first.IsExpanded = true;
    }

    public SonarGamingConfiguration DefaultSonarGamingConfiguration
    {
        get => _defaultSonarGamingConfiguration;
        set
        {
            if (Equals(value, _defaultSonarGamingConfiguration)) return;
            _defaultSonarGamingConfiguration = value;
            OnPropertyChanged(nameof(DefaultSonarGamingConfiguration));
        }
    }

    public ObservableCollection<AutoSwitchProfileViewModel> AutoSwitchProfiles
    {
        get => _autoSwitchProfiles;
        set
        {
            foreach (var p in _autoSwitchProfiles)
                p.PropertyChanged -= OnProfilePropertyChanged;
            _autoSwitchProfiles.CollectionChanged -= AutoSwitchProfilesOnCollectionChanged;

            _autoSwitchProfiles = value;

            foreach (var p in _autoSwitchProfiles)
                Subscribe(p);
            _autoSwitchProfiles.CollectionChanged += AutoSwitchProfilesOnCollectionChanged;

            if (_autoSwitchProfiles.FirstOrDefault() is { } first)
                first.IsExpanded = true;
        }
    }

    [JsonIgnore]
    public SonarGamingConfiguration ActiveProfile
    {
        get => _activeProfile;
        set
        {
            if (Equals(value, _activeProfile)) return;
            _activeProfile = value;
            OnPropertyChanged();
        }
    }

    public static HomeViewModel LoadHomeViewModel()
    {
        bool firstLoad = !StateManager.Instance.CheckStateExists<HomeViewModel>();
        var homeViewModel = StateManager.Instance.GetOrLoadState<HomeViewModel>();
        var steelSeriesSonarService = SteelSeriesSonarService.Instance;
        string selectedConfigId = steelSeriesSonarService.GetSelectedGamingConfiguration();
        var activeProfile = steelSeriesSonarService.GetGamingConfigurations()
            .FirstOrDefault(gc => gc.Id == selectedConfigId);
        if (firstLoad)
        {
            homeViewModel.DefaultSonarGamingConfiguration =
                activeProfile ?? homeViewModel.DefaultSonarGamingConfiguration;
        }

        homeViewModel.ActiveProfile = activeProfile ?? homeViewModel.DefaultSonarGamingConfiguration;
        return homeViewModel;
    }

    public void AddAutoSwitchProfile()
    {
        var profile = new AutoSwitchProfileViewModel();
        AutoSwitchProfiles.Add(profile); // Subscribe wired via CollectionChanged
        profile.IsExpanded = true;       // Accordion collapses others via OnProfilePropertyChanged
    }

    public void RemoveAutoSwitchProfile(AutoSwitchProfileViewModel profile)
    {
        AutoSwitchProfiles.Remove(profile);
        if (!AutoSwitchProfiles.Any())
        {
            var blank = new AutoSwitchProfileViewModel();
            AutoSwitchProfiles.Add(blank);
        }
        if (!AutoSwitchProfiles.Any(p => p.IsExpanded))
            AutoSwitchProfiles.First().IsExpanded = true;
    }

    private void Subscribe(AutoSwitchProfileViewModel profile)
    {
        profile.OnDeleteConfirmed = () => RemoveAutoSwitchProfile(profile);
        profile.PropertyChanged += OnProfilePropertyChanged;
    }

    private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AutoSwitchProfileViewModel.IsExpanded)) return;
        if (sender is not AutoSwitchProfileViewModel profile) return;

        if (profile.IsExpanded)
            foreach (var p in AutoSwitchProfiles.Where(p => p != profile))
                p.IsExpanded = false;
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        StateManager.Instance.SaveState<HomeViewModel>();
    }

    private void AutoSwitchProfilesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StateManager.Instance.SaveState<HomeViewModel>();
        if (e.NewItems != null)
            foreach (AutoSwitchProfileViewModel p in e.NewItems)
                Subscribe(p);
        if (e.OldItems != null)
            foreach (AutoSwitchProfileViewModel p in e.OldItems)
                p.PropertyChanged -= OnProfilePropertyChanged;
    }
}
