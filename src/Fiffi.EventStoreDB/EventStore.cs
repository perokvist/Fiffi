using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;

namespace Fiffi.EventStoreDB
{
    public class EventStore : IAdvancedEventStore
    {
        private readonly EventStoreClient client;
        private readonly Func<string, Type> typeResolver;

        public EventStore(EventStoreClient client, Func<string, Type> typeResolver)
        {
            this.client = client;
            this.typeResolver = typeResolver;

        }

        public async Task<long> AppendToStreamAsync(string streamName, long version, params IEvent[] events)
         => (await client.AppendToStreamAsync(streamName, StreamRevision.FromInt64(version), events.Select(x => x.ToEventData())))
            .NextExpectedVersion - 1;

        public async Task<long> AppendToStreamAsync(string streamName, params IEvent[] events)
         => (await client.AppendToStreamAsync(streamName, StreamRevision.None, events.Select(x => x.ToEventData())))
            .NextExpectedVersion - 1;

        public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var events = client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.FromInt64(version));
            if (await events.ReadState == ReadState.StreamNotFound)
                return (Enumerable.Empty<IEvent>(), 0);

            var r = events.Select(x => x.ToEvent(typeResolver));
            return (await r.ToArrayAsync(), (await events.LastAsync()).OriginalEventNumber.ToInt64());
        }
    }
}
