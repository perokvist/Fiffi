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
        private readonly Action<Exception, string, object[]> logger;

        public DaprEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore,
            Func<string, Type> typeResolver
            ) : this(eventStore, typeResolver, (ex, message, @params) => { })
        { }

        public DaprEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore,
            Func<string, Type> typeResolver,
            Action<Exception, string, object[]> logger
            )
        {
            this.eventStore = eventStore;
            this.typeResolver = typeResolver;
            this.logger = logger;
        }

        public async Task<long> AppendToStreamAsync(string streamName, IEvent[] events)
        {
            var attempt = 1;

            async Task<long> append(string sn, IEvent[] evts)
            {
                try
                {
                    return await eventStore.AppendToStreamAsync(streamName, events.Select(e => ToEventData(e)).ToArray());
                }
                catch (DBConcurrencyException ex)
                {
                    if (attempt > 2)
                    {
                        logger(ex, $"{ex.Message}. Attempt {0}", new object[] { attempt });
                        throw;
                    }

                    attempt++;
                    logger(ex, $"{ex.Message} - retries. Attempt {0}", new object[] { attempt });

                    return await append(streamName, events);
                }
            }

            return await append(streamName, events);
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params IEvent[] events)
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e)).ToArray());

        public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
            return (events.Select(e =>
            ToEvent(e.Data as string, typeResolver(e.EventName)) //TODO fix!
                .Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version }))),
                v);
        }

        public static IEvent ToEvent(string data, Type type)
       => (IEvent)JsonSerializer.Deserialize(data, type);

        public static EventData ToEventData(IEvent e)
            => new EventData(e.EventId().ToString(), e.Event.GetType().Name, e);
    }
}
