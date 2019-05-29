using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing;
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

        public FeedObserver(Func<IEvent[], Task> dispatcher, Func<string, Type> typeResolver)
        {
            this.dispatcher = dispatcher;
            this.typeResolver = typeResolver;
        }

        public Task CloseAsync(IChangeFeedObserverContext context, ChangeFeedObserverCloseReason reason)
            => Task.CompletedTask;

        public Task OpenAsync(IChangeFeedObserverContext context)
            => Task.CompletedTask;

        public async Task ProcessChangesAsync(IChangeFeedObserverContext context, IReadOnlyList<Document> docs, CancellationToken cancellationToken)
        {
            var events = docs
                .Where(d => d.GetPropertyValue<string>("type") == "Event")
                .Select(d => global::CosmoStore.CosmosDb.Conversion.documentToEventRead(d))
                .Select(d => d.ToEvent(this.typeResolver))
                .ToArray();
            await this.dispatcher(events);
            await context.CheckpointAsync();
        }
    }
}
