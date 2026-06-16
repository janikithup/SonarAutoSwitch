using System.Collections.Generic;
using System.Threading.Tasks;
using Sonar.AutoSwitch.Services;

namespace Sonar.AutoSwitch.Tests;

public class DelayedDeduplicateActionTest
{
    [Fact]
    public async Task Only_last_queued_action_runs()
    {
        var ran = new List<int>();
        var dda = new DelayedDeduplicateAction();
        dda.QueueAction(async () => { ran.Add(1); await Task.CompletedTask; }, delayInMs: 10);
        dda.QueueAction(async () => { ran.Add(2); await Task.CompletedTask; }, delayInMs: 10);
        dda.QueueAction(async () => { ran.Add(3); await Task.CompletedTask; }, delayInMs: 10);
        await Task.Delay(100);
        Assert.Equal([3], ran);
    }
}
