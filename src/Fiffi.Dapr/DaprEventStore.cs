using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class DaprEventStore : IAdvancedEventStore<EventData>
    {
        private readonly global::Dapr.EventStore.DaprEventStore eventStore;

        public DaprEventStore(global::Dapr.EventStore.DaprEventStore eventStore)
        {
            this.eventStore = eventStore;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params EventData[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e)).ToArray());

        public async Task<(IEnumerable<EventData> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var eventsAsync = eventStore.LoadEventStreamAsync(streamName, version);
            var events = await eventsAsync.ToArrayAsync();
            var ce = events
                .Select(ToEventData)
                .ToArray();
            return (ce, events.LastOrDefault()?.Version ?? (await eventStore.GetStreamMetaData(streamName)).Version);
        }

        public Task<long> AppendToStreamAsync(string streamName, params EventData[] events)
            => eventStore.AppendToStreamAsync(streamName, events.Select(ToEventData).ToArray());

        public IAsyncEnumerable<EventData> LoadEventStreamAsAsync(string streamName, long version)
        {
            var events = eventStore.LoadEventStreamAsync(streamName, version);
            return events
                .Select(ToEventData);
        }

        public static global::Dapr.EventStore.EventData ToEventData(EventData eventData)
            => new(eventData.EventId, eventData.EventName, eventData.Data, eventData.Version);

        public static EventData ToEventData(global::Dapr.EventStore.EventData eventData)
         => new(eventData.EventId, eventData.EventName, eventData.Data, eventData.Version);



    }
}
