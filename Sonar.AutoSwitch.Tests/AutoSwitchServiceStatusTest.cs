using Sonar.AutoSwitch.Services;
using Xunit;

namespace Sonar.AutoSwitch.Tests;

// U7 status dot: a completed switch sets the dot, but a switch that was canceled (superseded by a
// newer foreground change) must NOT be reported as a failure. Regression: the dot went red after a
// switch because a canceled PUT returned false and was treated as Disconnected.
public class AutoSwitchServiceStatusTest
{
    [Fact]
    public void Successful_switch_is_connected()
        => Assert.Equal(SonarConnectionStatus.Connected, AutoSwitchService.StatusForSwitch(switched: true, canceled: false));

    [Fact]
    public void Failed_switch_is_disconnected()
        => Assert.Equal(SonarConnectionStatus.Disconnected, AutoSwitchService.StatusForSwitch(switched: false, canceled: false));

    [Fact]
    public void Canceled_switch_leaves_the_dot_alone()
    {
        // The regression: a superseded switch returns false; treating that as Disconnected turned the dot red.
        Assert.Null(AutoSwitchService.StatusForSwitch(switched: false, canceled: true));
        Assert.Null(AutoSwitchService.StatusForSwitch(switched: true, canceled: true));
    }
}
