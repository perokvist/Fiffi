﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi
{
    public class EventStore : IEventStore
    {
        private readonly IEventStore<EventData> store;
        private readonly JsonSerializerOptions jsonSerializerOptions;
        private readonly Func<IEvent, JsonSerializerOptions, object> converter;
        private readonly Func<string, Type> typeResolver;
        private readonly Func<EventData, Type, JsonSerializerOptions, IEvent> toEvent;

        public EventStore(
            IEventStore<EventData> store,
            JsonSerializerOptions jsonSerializerOptions,
            Func<string, Type> typeResolver) :
            this(store, jsonSerializerOptions,
            Serialization.Extensions.AsMap(),
            typeResolver,
            ToEvent())
        { }

        public EventStore(IEventStore<EventData> store,
            JsonSerializerOptions jsonSerializerOptions,
            Func<IEvent, JsonSerializerOptions, object> converter,
            Func<string, Type> typeResolver,
            Func<EventData, Type, JsonSerializerOptions, IEvent> toEvent
            )
        {
            this.store = store;
            this.jsonSerializerOptions = jsonSerializerOptions;
            this.converter = converter;
            this.typeResolver = typeResolver;
            this.toEvent = toEvent;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params IEvent[] events)
         => store.AppendToStreamAsync(
             streamName,
             version,
             events.Select(e => ToEventData(e, x => this.converter(x, jsonSerializerOptions))).ToArray());

        public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var (events, streamVersion) = await store.LoadEventStreamAsync(streamName, version);

            var result = (events
                .Select(e => toEvent(e, typeResolver(e.EventName), this.jsonSerializerOptions)
                .Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version })))
                .AsEnumerable(),
                events.LastOrDefault()?.Version ?? streamVersion);

            return result;
        }

        public static EventData ToEventData(IEvent e, Func<IEvent, object> f)
            => new(e.EventId().ToString(), e.Event.GetType().Name, f(e));

        public static Func<EventData, Type, JsonSerializerOptions, IEvent> ToEvent()
        {
            var t = typeof(EventEnvelope<>);
            var methodInfo = typeof(EventDataExtensions).GetMethod(nameof(EventDataExtensions.EventAs), new[] { typeof(EventData), typeof(JsonSerializerOptions) });

            return (eventData, eventType, options) =>
            {
                var ft = t.MakeGenericType(eventType);
                var genericMethod = methodInfo.MakeGenericMethod(ft);
                var result = genericMethod.Invoke(null, new object[] { eventData, options });
                return result as IEvent;
            };
        }
    }

    public static class EventDataExtensions
    {
        public static T EventAs<T>(this EventData eventData, JsonSerializerOptions options = null)
            => eventData.Data switch
            {
                JsonElement d => ToObject<T>(d, options),
                T d => d,
                _ => throw new Exception($"Data was not of type {typeof(T).Name}")
            };

        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
        {
            var bufferWriter = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8JsonWriter(bufferWriter))
                element.WriteTo(writer);
            var result = JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options);
            return result;
        }
    }

    public record EventData(string EventId, string EventName, object Data, long Version = 0)
    {
        public static EventData Create(string EventName, object Data, long Version = 0) => new(Guid.NewGuid().ToString(), EventName, Data, Version);
    }
}