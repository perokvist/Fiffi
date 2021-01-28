using CloudNative.CloudEvents;
using Dapr.EventStore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class DaprCloudEventStore : IEventStore<CloudEvent>
    {
        private readonly global::Dapr.EventStore.DaprEventStore eventStore;
        private readonly Func<string, Type> typeResolver;
        private readonly Action<Exception, string, object[]> logger;

        public DaprCloudEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore,
            Func<string, Type> typeResolver
            ) : this(eventStore, typeResolver, (ex, message, @params) => { })
        { }

        public DaprCloudEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore,
            Func<string, Type> typeResolver,
            Action<Exception, string, object[]> logger
            )
        {
            this.eventStore = eventStore;
            this.typeResolver = typeResolver;
            this.logger = logger;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params CloudEvent[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e)).ToArray());

        public async Task<(IEnumerable<CloudEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
            var ce = events.Select(e =>
            ToEvent(e.Data, typeResolver(e.EventName)));
            return (ce, 0);
                //.Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version }))),
                //v);
        }

        public static CloudEvent ToEvent(string data, Type type)
       => (CloudEvent)JsonSerializer.Deserialize(data, type);

        public static EventData ToEventData(CloudEvent e)
           => new EventData
           {
               EventId = Guid.NewGuid(), //e.EventId(),
               EventName = e.GetType().Name,
               Data = JsonSerializer.Serialize<object>(e)
           };
    }
}
