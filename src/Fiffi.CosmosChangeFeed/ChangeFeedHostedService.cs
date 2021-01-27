using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.CosmosChangeFeed
{
    public class ChangeFeedHostedService : IHostedService
    {
        private readonly Func<CosmosClient> clientFactory;
        private readonly Func<CosmosClient, Task<ChangeFeedProcessor>> processorProvider;
        private readonly ILogger<ChangeFeedHostedService> logger;
        private CosmosClient client;
        private ChangeFeedProcessor processor;

        public ChangeFeedHostedService(
            Func<CosmosClient> clientFactory,
            Func<CosmosClient, Task<ChangeFeedProcessor>> processorProvider,
            ILogger<ChangeFeedHostedService> logger)
        {
            this.clientFactory = clientFactory;
            this.processorProvider = processorProvider;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.client = clientFactory();
            logger.LogInformation($"Starting {nameof(ChangeFeedHostedService)} - {client.Endpoint}");
            this.processor = await this.processorProvider(this.client);
            await this.processor.StartAsync();
            logger.LogInformation($"Started {nameof(ChangeFeedHostedService)}");
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Stopping {nameof(ChangeFeedHostedService)}");
            await this.processor.StopAsync();
            logger.LogInformation($"Stopped {nameof(ChangeFeedHostedService)}");
            this.client.Dispose();
        }
    }
}
