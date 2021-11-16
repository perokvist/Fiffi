using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi;

public static class Polling
{
    public static async Task PollAsync(CancellationToken stoppingToken, Func<Func<Task<bool>>, Task> poll, int delay = 50)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (await PollAsync(poll))
                await Task.Delay(delay, stoppingToken);
        }
    }

    static async Task<bool> PollAsync(Func<Func<Task<bool>>, Task> poll)
    {
        var wait = true;
        await poll(() => Task.Factory.StartNew(() => wait = false));
        return wait;
    }
}
