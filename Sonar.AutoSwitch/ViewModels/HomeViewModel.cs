using System;
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
    // Search + sort: ephemeral view state, not persisted to JSON.
    private string _searchText = string.Empty;
    private int _sortDirection; // 0 = manual order, 1 = A→Z, -1 = Z→A

    public static IReadOnlyList<string> ProcessNames { get; } =
        Process.GetProcesses().Select(p => p.ProcessName).Distinct().OrderBy(x => x).ToList();

    public HomeViewModel()
    {
        foreach (var p in _autoSwitchProfiles)
            Subscribe(p);
        _autoSwitchProfiles.CollectionChanged += AutoSwitchProfilesOnCollectionChanged;
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

    [JsonIgnore]
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            // base.OnPropertyChanged bypasses the override that calls SaveState.
            base.OnPropertyChanged(nameof(SearchText));
            base.OnPropertyChanged(nameof(FilteredProfiles));
        }
    }

    [JsonIgnore]
    public IEnumerable<AutoSwitchProfileViewModel> FilteredProfiles
    {
        get
        {
            IEnumerable<AutoSwitchProfileViewModel> source = _sortDirection switch
            {
                1  => _autoSwitchProfiles.OrderBy(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase),
                -1 => _autoSwitchProfiles.OrderByDescending(p => p.DisplayName, StringComparer.CurrentCultureIgnoreCase),
                _  => _autoSwitchProfiles
            };
            if (string.IsNullOrWhiteSpace(_searchText))
                return source is ObservableCollection<AutoSwitchProfileViewModel> ? source : source.ToList();
            var term = _searchText.Trim();
            return source.Where(p =>
                p.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.ExeName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                p.Title.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    [JsonIgnore] public bool SortAscendingActive => _sortDirection == 1;
    [JsonIgnore] public bool SortDescendingActive => _sortDirection == -1;

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

    public void SortAscending()
    {
        _sortDirection = _sortDirection == 1 ? 0 : 1;
        base.OnPropertyChanged(nameof(FilteredProfiles));
        base.OnPropertyChanged(nameof(SortAscendingActive));
        base.OnPropertyChanged(nameof(SortDescendingActive));
    }

    public void SortDescending()
    {
        _sortDirection = _sortDirection == -1 ? 0 : -1;
        base.OnPropertyChanged(nameof(FilteredProfiles));
        base.OnPropertyChanged(nameof(SortAscendingActive));
        base.OnPropertyChanged(nameof(SortDescendingActive));
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
        if (e.PropertyName == nameof(AutoSwitchProfileViewModel.IsExpanded))
        {
            if (sender is not AutoSwitchProfileViewModel profile) return;
            if (profile.IsExpanded)
                foreach (var p in AutoSwitchProfiles.Where(p => p != profile))
                    p.IsExpanded = false;
            return;
        }
        // Refresh filtered view when sort is active and a profile name changes.
        if (e.PropertyName is nameof(AutoSwitchProfileViewModel.ExeName)
                           or nameof(AutoSwitchProfileViewModel.Title))
        {
            if (_sortDirection != 0)
                base.OnPropertyChanged(nameof(FilteredProfiles));
        }
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName is nameof(FilteredProfiles)
                         or nameof(SortAscendingActive)
                         or nameof(SortDescendingActive))
            return;
        StateManager.Instance.SaveState<HomeViewModel>();
    }

    private void AutoSwitchProfilesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        StateManager.Instance.SaveState<HomeViewModel>();
        base.OnPropertyChanged(nameof(FilteredProfiles));
        if (e.NewItems != null)
            foreach (AutoSwitchProfileViewModel p in e.NewItems)
                Subscribe(p);
        if (e.OldItems != null)
            foreach (AutoSwitchProfileViewModel p in e.OldItems)
                p.PropertyChanged -= OnProfilePropertyChanged;
    }
}
