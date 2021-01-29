using CloudNative.CloudEvents;
using Dapr.EventStore;
using Fiffi.CloudEvents;
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
        private readonly Action<Exception, string, object[]> logger;

        public DaprCloudEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore
            ) : this(eventStore, (ex, message, @params) => { })
        { }

        public DaprCloudEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore,
            Action<Exception, string, object[]> logger
            )
        {
            this.eventStore = eventStore;
            this.logger = logger;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params CloudEvent[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e)).ToArray());

        public async Task<(IEnumerable<CloudEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
            var ce = events.Select(e =>
            ToEvent(e.Data as byte[])); //TODO fix!
            return (ce, v);
                //.Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version }))),
                //v);
        }

        public static CloudEvent ToEvent(byte[] data)
            => new JsonEventFormatter().DecodeStructuredEvent(data, new EventMetaDataExtension());

        public static EventData ToEventData(CloudEvent e)
           => new EventData(e.Id, e.Type, new JsonEventFormatter().EncodeStructuredEvent(e, out _));
    }
}
