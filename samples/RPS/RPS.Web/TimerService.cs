using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RPS.Web
{
    public sealed class TimerService : IHostedService, IDisposable
    {
        private readonly Func<Task> action;
        private readonly TimeSpan interval;
        private readonly ILogger<TimerService> logger;
        private Timer timer;

        public TimerService(Func<Task> action, TimeSpan interval, ILogger<TimerService> logger)
        {
            this.action = action;
            this.interval = interval;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Timed Hosted Service running.");

            timer = new Timer(async (state) =>
            {
                logger.LogInformation($"{nameof(TimerService)} triggered.");
                await action();
            }, null, TimeSpan.Zero,
                interval);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Timed Hosted Service is stopping.");
            timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
        public void Dispose()
         => timer?.Dispose();
    }
}
