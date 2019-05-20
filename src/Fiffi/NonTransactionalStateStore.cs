using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
    public class NonTransactionalStateStore : IStateStore
    {
        private readonly IEventStore store;
        private readonly IStreamOutbox streamOutbox;

        public NonTransactionalStateStore(IEventStore store, IStreamOutbox streamOutbox)
        {
            this.store = store;
            this.streamOutbox = streamOutbox;
        }
        public async Task CompleteOutBoxAsync(string sourceId, params IEvent[] events)
            => await this.streamOutbox.CompleteAsync(sourceId, events);

        public async Task<IEvent[]> GetAllUnPublishedEventsAsync()
            => (await Task.WhenAll((await streamOutbox.GetAllPendingAsync()).Select(x => GetOutBoxAsync(x.SourceId)))).SelectMany(x => x).ToArray();

        public async Task<(T State, long Version)> GetAsync<T>(IAggregateId id)
            where T : class, new()
        {
            var streamName = typeof(T).Name.AsStreamName(id).StreamName;
            var happend = await this.store.LoadEventStreamAsync(streamName, 0);
            if (!happend.Events.Any()) return (null, 0);
            return (happend.Events.Rehydrate<T>(), happend.Version);
        }

        public async Task<IEvent[]> GetOutBoxAsync(string sourceId)
        {
            var pending = await streamOutbox.GetPendingAsync(sourceId);
            return (await this.store.LoadEventStreamAsync(pending.StreamName, pending.Version)).Events.ToArray();
        }

        public async Task SaveAsync<T>(IAggregateId id, T state, long version, IEvent[] events)
            where T : class, new()
        {
            var streamName = typeof(T).Name.AsStreamName(id).StreamName;
            await streamOutbox.PendingAsync(id, streamName, version, events);

            try
            {
                await this.store.AppendToStreamAsync(streamName, version, events);
            }
            catch (DBConcurrencyException)
            {
                await streamOutbox.CancelAsync(id.Id, events);
                throw;
            }
        }
    }
}
