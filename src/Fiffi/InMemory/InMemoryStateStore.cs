using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
    public class InMemoryStateStore : IStateStore
    {
        private readonly IEventStore store;
        private IDictionary<string, (string StreamName, long Version)> published = new ConcurrentDictionary<string, (string StreamName, long Version)>();

        public InMemoryStateStore() : this(new InMemoryEventStore())
        { }

        public InMemoryStateStore(IEventStore store)
        {
            this.store = store;
        }
        public Task CompleteOutBoxAsync(params IEvent[] events)
        => Task.WhenAll(events
                 .GroupBy(e => e.SourceId)
                 .Select(async e =>
                 {
                     if (!published.ContainsKey(e.Key)) return;
                     var version = (await GetOutBoxAsync(e.Key)).Last().Meta.GetEventStoreMetaData().EventVersion;
                     published[e.Key] = (published[e.Key].StreamName, version);
                 }));

        public async Task<IEvent[]> GetAllUnPublishedEventsAsync()
            => (await Task.WhenAll(published.Select(x => GetOutBoxAsync(x.Key)))).SelectMany(x => x).ToArray();

        public async Task<(T State, long Version)> GetAsync<T>(IAggregateId id)
            where T : class, new()
        {
            var happend = await this.store.LoadEventStreamAsync(typeof(T).Name.AsStreamName(id).StreamName, 0);
            if (!happend.Events.Any()) return (null, 0);
            return (happend.Events.Rehydrate<T>(), happend.Version);
        }

        async Task<IEvent[]> GetOutBoxAsync(string sourceId)
        {
            if (!published.ContainsKey(sourceId)) Array.Empty<IEvent>();
            return (await this.store.LoadEventStreamAsync(published[sourceId].StreamName, published[sourceId].Version)).Events.ToArray();
        }

        public Task SaveAsync<T>(IAggregateId id, T state, long version, IEvent[] events)
            where T : class, new()
        {
            var streamName = typeof(T).Name.AsStreamName(id).StreamName;
            if (!this.published.ContainsKey(id.Id)) this.published.Add(id.Id, (streamName, 0));
            return this.store.AppendToStreamAsync(streamName, version, events);
        }
    }
}
