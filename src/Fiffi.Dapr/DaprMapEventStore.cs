using Dapr.EventStore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class DaprMapEventStore : IEventStore<IDictionary<string, object>>
    {
        private readonly global::Dapr.EventStore.DaprEventStore eventStore;

        public Func<IDictionary<string, object>, string> IdProvider { get; set; } = d => d["id"].ToString();
        public Func<IDictionary<string, object>, string> NameProvider { get; set; } = d => d["type"].ToString();

        public DaprMapEventStore(global::Dapr.EventStore.DaprEventStore eventStore)
        {
            this.eventStore = eventStore;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params IDictionary<string, object>[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e)).ToArray());

        public async Task<(IEnumerable<IDictionary<string, object>> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var eventsAsync = eventStore.LoadEventStreamAsync(streamName, version);
            var events = await eventsAsync.ToArrayAsync();
            var ce = events
                .Select(e => e.Data)
                .Cast<IDictionary<string, object>>()
                .ToArray();
            return (ce, events.LastOrDefault()?.Version ?? 0);
        }

        public global::Dapr.EventStore.EventData ToEventData(IDictionary<string, object> e)
        => new(IdProvider(e), NameProvider(e) , e);
    }
}
