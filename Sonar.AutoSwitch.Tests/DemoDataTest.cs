using System.Linq;
using Sonar.AutoSwitch;
using Sonar.AutoSwitch.Services;
using Xunit;

namespace Sonar.AutoSwitch.Tests;

public class DemoDataTest
{
    // Build the demo VM the same way App does in --demo: point config reads at the demo presets
    // first, so the exe auto-match fires against them. Restores the real query afterward.
    private static HomeViewModelResult Build()
    {
        var svc = SteelSeriesSonarService.Instance;
        svc.ConfigQuery = () => DemoData.Configs;
        svc.RefreshGamingConfigurations();
        var vm = DemoData.HomeViewModel();
        return new HomeViewModelResult(vm, svc);
    }

    private sealed class HomeViewModelResult : System.IDisposable
    {
        public readonly ViewModels.HomeViewModel Vm;
        private readonly SteelSeriesSonarService _svc;
        public HomeViewModelResult(ViewModels.HomeViewModel vm, SteelSeriesSonarService svc) { Vm = vm; _svc = svc; }
        public void Dispose()
        {
            _svc.ConfigQuery = _svc.GetGamingConfigurations; // restore real query for other tests
            _svc.RefreshGamingConfigurations();
        }
    }

    [Fact]
    public void HomeViewModel_is_a_clean_four_profile_demo_first_card_expanded_and_connected()
    {
        using var r = Build();
        var vm = r.Vm;

        Assert.True(vm.IsDemo);
        Assert.Equal(4, vm.AutoSwitchProfiles.Count);

        Assert.True(vm.AutoSwitchProfiles[0].IsExpanded);
        Assert.All(vm.AutoSwitchProfiles.Skip(1), p => Assert.False(p.IsExpanded));

        Assert.Equal(SonarConnectionStatus.Connected, vm.SonarStatus);
        Assert.Equal("Cyberpunk 2077", vm.ActiveProfile.Name);

        Assert.All(vm.AutoSwitchProfiles, p => Assert.False(p.IsIncomplete));
        Assert.False(vm.AutoSwitchProfiles[0].IsAdvancedExpanded);
    }

    [Fact]
    public void Expanded_card_shows_the_autofill_hint_from_a_real_match()
    {
        using var r = Build();
        var cyberpunk = r.Vm.AutoSwitchProfiles[0];

        // The screenshot's job: showcase auto-match. Exe "Cyberpunk2077" matched preset "Cyberpunk 2077".
        Assert.Equal("Cyberpunk 2077", cyberpunk.SonarGamingConfiguration.Name);
        Assert.True(cyberpunk.HasSonarMatchHint);
        Assert.Contains("Auto-matched", cyberpunk.SonarMatchHint);
        Assert.Contains("Cyberpunk 2077", cyberpunk.SonarMatchHint);
    }
}
