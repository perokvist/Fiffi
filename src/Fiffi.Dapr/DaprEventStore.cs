using Dapr.EventStore;
using Fiffi.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class DaprEventStore : IAdvancedEventStore
    {
        private readonly global::Dapr.EventStore.DaprEventStore eventStore;
        private readonly Func<string, Type> typeResolver;
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly Action<Exception, string, object[]> logger;

        public DaprEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore,
            JsonSerializerOptions jsonSerializerOptions,
            Func<string, Type> typeResolver
            ) : this(eventStore, typeResolver, jsonSerializerOptions, (ex, message, @params) => { })
        { }

        public DaprEventStore(
            global::Dapr.EventStore.DaprEventStore eventStore,
            Func<string, Type> typeResolver,
            JsonSerializerOptions jsonSerializerOptions,
            Action<Exception, string, object[]> logger
            )
        {
            this.eventStore = eventStore.Tap(x => x.MetaProvider = streamName => new Dictionary<string, string>
                    {
                        { "partitionKey", streamName }
                    })
                    .Tap(x => x.StoreName = "localcosmos");
            this.typeResolver = typeResolver;
            this.jsonSerializerOptions = jsonSerializerOptions;
            this.logger = logger;
        }

        public async Task<long> AppendToStreamAsync(string streamName, IEvent[] events)
        {
            var attempt = 1;

            async Task<long> append(string sn, IEvent[] evts)
            {
                try
                {
                    return await eventStore.AppendToStreamAsync(streamName, events.Select(e => ToEventData(e, this.jsonSerializerOptions)).ToArray());
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
         => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => ToEventData(e, this.jsonSerializerOptions)).ToArray());

        public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
            var toEvent = ToEvent();

            var result = (events
                .Select(e => toEvent(e, typeResolver(e.EventName), this.jsonSerializerOptions)
                .Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version }))),
                v);
            return result;
        }

        public static Func<EventData, Type, JsonSerializerOptions, IEvent> ToEvent()
        {
            var t = typeof(EventEnvelope<>);
            var methodInfo = typeof(global::Dapr.EventStore.Extensions).GetMethod(nameof(global::Dapr.EventStore.Extensions.EventAs), new[] { typeof(EventData), typeof(JsonSerializerOptions) });

            return (eventData, eventType, options) =>
            {
                var ft = t.MakeGenericType(eventType);
                var genericMethod = methodInfo.MakeGenericMethod(ft);
                var result = genericMethod.Invoke(null, new object[] { eventData, options });
                return result as IEvent;
            };
        }

        public static EventData ToEventData(IEvent e, JsonSerializerOptions options)
        {
            var data = JsonSerializer.Serialize(e, options).ToMap(options);
            return new EventData(e.EventId().ToString(), e.Event.GetType().Name, data);
        }
    }
}
