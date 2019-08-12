using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore
{
    public class ChangeFeedHostedService : IHostedService
    {
        private readonly Func<Task<IChangeFeedProcessor>> processorProvider;
        private readonly ILogger<ChangeFeedHostedService> logger;
        private IChangeFeedProcessor processor;

        public ChangeFeedHostedService(Func<Task<IChangeFeedProcessor>> processorProvider, ILogger<ChangeFeedHostedService> logger)
        {
            this.processorProvider = processorProvider;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting {nameof(ChangeFeedHostedService)}");
            this.processor = await this.processorProvider();
            await this.processor.StartAsync();
            logger.LogInformation($"Started {nameof(ChangeFeedHostedService)}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {nameof(ChangeFeedHostedService)}");
            await this.processor.StopAsync();
            logger.LogInformation($"Stopped {nameof(ChangeFeedHostedService)}");
        }
    }
}
