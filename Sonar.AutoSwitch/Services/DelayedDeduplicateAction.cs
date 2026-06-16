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
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayInMs, token);
                await action();
            }
            catch (OperationCanceledException) { }
        });
    }
}
