using Sonar.AutoSwitch.Services;
using Sonar.AutoSwitch.ViewModels;

namespace Sonar.AutoSwitch.Tests;

public class AutoSwitchServiceKeepWhileRunningTest
{
    private static AutoSwitchProfileViewModel Profile(string exe = "SoTGame") =>
        new() { ExeName = exe };

    [Fact]
    public void Keeps_when_global_on_and_exe_running()
        => Assert.True(AutoSwitchService.ShouldKeepLocked(Profile(), keepWhileRunning: true, _ => true));

    [Fact]
    public void Does_not_keep_when_exe_not_running()
        => Assert.False(AutoSwitchService.ShouldKeepLocked(Profile(), keepWhileRunning: true, _ => false));

    [Fact]
    public void Does_not_keep_when_global_off()
        => Assert.False(AutoSwitchService.ShouldKeepLocked(Profile(), keepWhileRunning: false, _ => true));

    [Fact]
    public void Does_not_keep_when_locked_is_null()
        => Assert.False(AutoSwitchService.ShouldKeepLocked(null, keepWhileRunning: true, _ => true));

    [Fact]
    public void Does_not_keep_when_ExeName_empty()
        => Assert.False(AutoSwitchService.ShouldKeepLocked(Profile(exe: ""), keepWhileRunning: true, _ => true));

    [Fact]
    public void Does_not_keep_when_profile_disabled()
        => Assert.False(AutoSwitchService.ShouldKeepLocked(new AutoSwitchProfileViewModel { ExeName = "SoTGame", IsEnabled = false }, keepWhileRunning: true, _ => true));
}
