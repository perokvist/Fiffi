using CloudNative.CloudEvents;
using Fiffi.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.CloudEvents
{
    public class CloudEventStore : IEventStore<CloudEvent>
    {
        private readonly IEventStore<IDictionary<string, object>> eventStore;

        private readonly Action<Exception, string, object[]> logger;

        public CloudEventStore(
            IEventStore<IDictionary<string, object>> eventStore
            ) : this(eventStore, (ex, message, @params) => { })
        { }

        public CloudEventStore(
            IEventStore<IDictionary<string, object>> eventStore,
            Action<Exception, string, object[]> logger
            )
        {
            this.eventStore = eventStore;
            this.logger = logger;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params CloudEvent[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToMapData(e)).ToArray());

        public async Task<(IEnumerable<CloudEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
            var ce = events.Select(e => e.ToEvent());
            return (ce, v);
            //.Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version }))),
            //v);
        }

        public static IDictionary<string, object> ToMapData(CloudEvent e)
            => e.ToJson().ToMap();

        public static IEventStore<CloudEvent> CreateInMemoryStore()
            => new InMemory.InMemoryEventStore<CloudEvent>(
                x => x.Extension<EventStoreMetaDataExtension>().MetaData.EventVersion,
                (e, v) => {
                    var ext = e.Extension<EventStoreMetaDataExtension>();
                    ext.MetaData = ext.MetaData with { EventVersion = v };
                    ext.Attach(e);
                    },
                x => x.Extension<EventMetaDataExtension>().MetaData.EventId
                );

    }
}
