using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore
{
    public class FeedObserver : IChangeFeedObserver
    {
        private readonly Func<IEvent[], Task> dispatcher;
        private readonly Func<string, Type> typeResolver;
        private readonly ILogger logger;

        public FeedObserver(Func<IEvent[], Task> dispatcher, Func<string, Type> typeResolver, ILogger logger)
        {
            this.dispatcher = dispatcher;
            this.typeResolver = typeResolver;
            this.logger = logger;
        }

        public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
        {
            this.logger.LogInformation($"{nameof(FeedObserver)} {context.PartitionKeyRangeId} closing due to {reason}.");
            return Task.CompletedTask;
        }

        public Task OpenAsync(IChangeFeedObserverContext context)
        {
            this.logger.LogInformation($"{nameof(FeedObserver)} {context.PartitionKeyRangeId} opening.");
            return Task.CompletedTask;
        }

        public async Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
        {
            var events = docs
                .Where(d => d.GetPropertyValue<string>("type") == "Event")
                //.Select(d => global::CosmoStore.Conversion.eventWriteToEventRead<JToken, long>())
                //.Select(d => global::CosmoStore.EventRead<JToken, long>. (d))
                //.Select(d => d.ToEvent(this.typeResolver))
                .Cast<IEvent>() //TODO :)
                .ToArray();
            try
            {
                await this.dispatcher(events);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"{nameof(FeedObserver)} error : {ex.Message}", events);
                throw;
            }
            await context.CheckpointAsync();
        }
    }
}
