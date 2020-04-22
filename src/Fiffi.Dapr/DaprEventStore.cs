using Dapr.EventStore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class DaprEventStore : IAdvancedEventStore
    {
        private readonly global::Dapr.EventStore.DaprEventStore eventStore;
        private readonly Func<string, Type> typeResolver;

        public DaprEventStore(global::Dapr.EventStore.DaprEventStore eventStore,
            Func<string, Type> typeResolver)
        {
            this.eventStore = eventStore;
            this.typeResolver = typeResolver;
        }

        public Task<long> AppendToStreamAsync(string streamName, IEvent[] events)
         => eventStore.AppendToStreamAsync(streamName, events.Select(e => ToEventData(e)).ToArray());

        public Task<long> AppendToStreamAsync(string streamName, long version, params IEvent[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e)).ToArray());

        public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
            return (events.Select(e =>
            ToEvent(e.Data, typeResolver(e.EventName))
                .Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version }))),
                v);
        }

        public static IEvent ToEvent(string data, Type type)
            => (IEvent)JsonSerializer.Deserialize(data, type);

        public static EventData ToEventData(IEvent e)
            => new EventData {
                EventId = e.EventId(),
                EventName = e.GetType().Name,
                Data = JsonSerializer.Serialize<object>(e)
            };

    }
}
