using Dapr.EventStore;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class DaprCloudEventStore : IEventStore<IDictionary<string, object>>
    {
        private readonly global::Dapr.EventStore.DaprEventStore eventStore;

        public DaprCloudEventStore(global::Dapr.EventStore.DaprEventStore eventStore)
        {
            this.eventStore = eventStore;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params IDictionary<string, object>[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e)).ToArray());

        public async Task<(IEnumerable<IDictionary<string, object>> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
            var ce = events
                .Select(e => e.Data)
                .Cast<IDictionary<string, object>>()
                .ToArray();
            return (ce, v);
        }

        public static EventData ToEventData(IDictionary<string, object> e)
        => new(e["id"].ToString(), e["type"].ToString(), e);
    }
}
