using Fiffi.Serialization;
using System;
using System.Buffers;

namespace Fiffi;

public class AdvancedEventStore : EventStore, IAdvancedEventStore
{
    private readonly IAdvancedEventStore<EventData> store;

    public AdvancedEventStore(IAdvancedEventStore<EventData> store,
        JsonSerializerOptions jsonSerializerOptions,
        Func<string, Type> typeResolver) : base(store, jsonSerializerOptions, typeResolver)
    {
        this.store = store;
    }

    public AdvancedEventStore(IAdvancedEventStore<EventData> store,
        JsonSerializerOptions jsonSerializerOptions,
        Func<IEvent, JsonSerializerOptions, object> converter,
        Func<string, Type> typeResolver,
        Func<EventData, Type, JsonSerializerOptions, IEvent> toEvent) : base(store, jsonSerializerOptions, converter, typeResolver, toEvent)
    {
        this.store = store;
    }

    public Task<long> AppendToStreamAsync(string streamName, params IEvent[] events)
        => store.AppendToStreamAsync(streamName, events.Select(e => ToEventData(e, x => this.converter(x, jsonSerializerOptions))).ToArray());

    public async IAsyncEnumerable<IEvent> LoadEventStreamAsAsync(string streamName, long version)
    {
        var events = store.LoadEventStreamAsAsync(streamName, version);
        await foreach (var item in events)
        {
            yield return toEvent(item, typeResolver(item.EventName), jsonSerializerOptions)
            .Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = item.Version, EventPosition = item.Version }));
        }
    }

    public async IAsyncEnumerable<IEvent> LoadEventStreamAsAsync(string streamName, params IStreamFilter[] filters)
    {
        var events = store.LoadEventStreamAsAsync(streamName, filters);
        await foreach (var item in events)
        {
            yield return toEvent(item, typeResolver(item.EventName), jsonSerializerOptions)
            .Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = item.Version, EventPosition = item.Version }));
        }
    }
}

public class EventStore : IEventStore
{
    private readonly IEventStore<EventData> store;
    protected readonly JsonSerializerOptions jsonSerializerOptions;
    protected readonly Func<IEvent, JsonSerializerOptions, object> converter;
    protected readonly Func<string, Type> typeResolver;
    protected readonly Func<EventData, Type, JsonSerializerOptions, IEvent> toEvent;

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
        => new(e.GetStreamName(), e.EventId().ToString(), e.Event.GetType().Name, f(e), DateTime.UtcNow);

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
            JsonElement d => d.ToObject<T>(options),
            IDictionary<string, object> d => d.ToObject<T>(options),
            T d => d,
            _ => throw new Exception($"Data was not of type {typeof(T).Name}")
        };


}

public record EventData(
    string EventStreamId,
    string EventId,
    string EventName,
    object Data,
    DateTime Created,
    long Version = 0)

{
    public static EventData Create(string EventStreamId, string EventName, object Data, long Version = 0) => new(EventStreamId, Guid.NewGuid().ToString(), EventName, Data, DateTime.UtcNow, Version);
}
