using System.Collections.Generic;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Tests;

public class HomeViewModelTest
{
    // B9 regression: FilteredProfiles must not NRE when ExeName or Title is null.
    [Fact]
    public void FilteredProfiles_does_not_throw_when_profile_has_null_ExeName_or_Title()
    {
        var home = new HomeViewModel();
        home.AutoSwitchProfiles.Clear();
        home.AutoSwitchProfiles.Add(new AutoSwitchProfileViewModel { ExeName = null!, Title = null! });
        home.AutoSwitchProfiles.Add(new AutoSwitchProfileViewModel { ExeName = "game", Title = null! });

        home.SearchText = "game";

        // FilteredProfiles should not throw and should return only profiles matching "game"
        var results = home.FilteredProfiles;
        Assert.NotEmpty(results);
    }
}
