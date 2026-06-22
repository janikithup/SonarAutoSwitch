using Sonar.AutoSwitch.Services;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Tests;

public class AppViewModelTest
{
    [Fact]
    public void TrayTooltip_shows_active_profile_name_when_set()
    {
        var home = new HomeViewModel();
        home.ActiveProfile = new SonarGamingConfiguration("id1", "Hell Yeah");
        var settings = new SettingsViewModel();
        var vm = new AppViewModel();
        vm.WireTooltip(home, settings);

        Assert.Contains("Hell Yeah", vm.TrayTooltipText);
        Assert.DoesNotContain("disabled", vm.TrayTooltipText);
    }

    [Fact]
    public void TrayTooltip_shows_base_text_when_no_active_profile()
    {
        var home = new HomeViewModel();
        var settings = new SettingsViewModel();
        var vm = new AppViewModel();
        vm.WireTooltip(home, settings);

        Assert.Equal("Sonar Auto Switch", vm.TrayTooltipText);
    }

    [Fact]
    public void TrayTooltip_updates_when_active_profile_changes()
    {
        var home = new HomeViewModel();
        var settings = new SettingsViewModel();
        var vm = new AppViewModel();
        vm.WireTooltip(home, settings);
        var before = vm.TrayTooltipText;

        home.ActiveProfile = new SonarGamingConfiguration("id1", "Sea of Thieves");

        Assert.NotEqual(before, vm.TrayTooltipText);
        Assert.Contains("Sea of Thieves", vm.TrayTooltipText);
    }

    [Fact]
    public void TrayTooltip_shows_disabled_when_service_off()
    {
        var home = new HomeViewModel();
        var settings = new SettingsViewModel { Enabled = false };
        var vm = new AppViewModel();
        vm.WireTooltip(home, settings);

        Assert.Contains("disabled", vm.TrayTooltipText);
    }

    [Fact]
    public void TrayTooltip_updates_when_enabled_toggled()
    {
        var home = new HomeViewModel();
        var settings = new SettingsViewModel { Enabled = true };
        var vm = new AppViewModel();
        vm.WireTooltip(home, settings);

        settings.Enabled = false;

        Assert.Contains("disabled", vm.TrayTooltipText);
    }

    [Fact]
    public void StartupDescription_returns_string()
    {
        var vm = new SettingsViewModel();
        Assert.NotNull(vm.StartupDescription);
        Assert.NotEqual(string.Empty, vm.StartupDescription);
    }
}
