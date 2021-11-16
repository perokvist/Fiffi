using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi.CosmoStore;

public static class Extensions
{
    public static global::CosmoStore.EventWrite<JToken> ToCosmosStoreEvent(this IEvent @event)
     => @event.Meta.GetEventMetaData().Pipe(meta => new global::CosmoStore.EventWrite<JToken>(
            meta.EventId,
            meta.CorrelationId,
            meta.CausationId,
            @event.GetEventName(),
            JToken.FromObject(@event.Event)
                .Tap(token => token[nameof(IEvent.Meta)].Replace(JToken.FromObject(new Dictionary<string, string>()))),
            JToken.FromObject(@event.Meta)));

    public static IEvent ToEvent(this global::CosmoStore.EventRead<JToken, long> eventRead, Func<string, Type> typeResolver)
     => typeResolver(eventRead.Name)
        .Pipe(t => ((EventRecord)eventRead.Data.ToObject(t))
        .Pipe(e => EventEnvelope.Create(eventRead.Id.ToString(), e))
        .Tap(e => e.Meta = eventRead.Metadata.Value.ToObject<Dictionary<string, string>>())
        .Tap(e => e.Meta.AddStoreMetaData(new EventStoreMetaData { EventPosition = eventRead.Version, EventVersion = eventRead.Version })));
}

public static class ConversionExtensions
{
    public static global::CosmoStore.EventRead<JToken, long> ToEventRead(this JToken doc)
        => new global::CosmoStore.EventRead<JToken, long>(
            doc.Value<Guid>("id"),
            doc.Value<Guid>("correlationId"),
            doc.Value<Guid>("causationId"),
            doc.Value<string>("streamId"),
            doc.Value<long>("version"),
            doc.Value<string>("name"),
            doc.Value<JToken>("data"),
            doc.Value<JToken>("metadata"),
            doc.Value<DateTime>("createdUtc")
            );

    public static IEvent[] ToEvents(this IEnumerable<JToken> documents, Func<string, Type> typeResolver)
     => documents
           .Where(d => d.Value<string>("type") == "Event")
           .Select(d => d.ToEventRead())
           .Select(d => d.ToEvent(typeResolver))
           .ToArray();
}
