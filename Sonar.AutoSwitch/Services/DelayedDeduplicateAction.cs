using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sonar.AutoSwitch.Services;

public class DelayedDeduplicateAction
{
    private CancellationTokenSource? _cts;

    public void QueueAction(Func<Task> action, int delayInMs = 2000)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _ = Task.Delay(delayInMs, _cts.Token).ContinueWith(
            _ => action(),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion,
            TaskScheduler.Default);
    }
}
