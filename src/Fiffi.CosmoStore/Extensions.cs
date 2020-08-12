using Microsoft.Azure.Documents;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi.CosmoStore
{
    public static class Extensions
    {
        public static global::CosmoStore.EventWrite<JToken> ToCosmosStoreEvent(this IEvent @event)
         => @event.Meta.GetEventMetaData().Pipe(meta => new global::CosmoStore.EventWrite<JToken>(
                meta.EventId,
                meta.CorrelationId,
                meta.CausationId,
                @event.GetEventName(),
                JToken.FromObject(@event)
                    .Tap(token => token[nameof(IEvent.Meta)].Replace(JToken.FromObject(new Dictionary<string, string>()))),
                JToken.FromObject(@event.Meta)));

        public static IEvent ToEvent(this global::CosmoStore.EventRead<JToken, long> eventRead, Func<string, Type> typeResolver)
         => typeResolver(eventRead.Name)
            .Pipe(t => ((IEvent)eventRead.Data.ToObject(t))
            .Tap(e => e.Meta = eventRead.Metadata.Value.ToObject<Dictionary<string, string>>())
            .Tap(e => e.Meta.AddStoreMetaData(new EventStoreMetaData { EventPosition = eventRead.Version, EventVersion = eventRead.Version })));
    }

    public static class ConversionExtensions
    {
        public static global::CosmoStore.EventRead<JToken, long> ToEventRead(this Document doc)
            => new global::CosmoStore.EventRead<JToken, long>(
                doc.GetPropertyValue<Guid>("id"),
                doc.GetPropertyValue<Guid>("correlationId"),
                doc.GetPropertyValue<Guid>("causationId"),
                doc.GetPropertyValue<string>("streamId"),
                doc.GetPropertyValue<long>("version"),
                doc.GetPropertyValue<string>("name"),
                doc.GetPropertyValue<JToken>("data"),
                doc.GetPropertyValue<JToken>("metadata"),
                doc.GetPropertyValue<DateTime>("createdUtc")
                );

        public static IEvent[] ToEvents(this IEnumerable<Document> documents, Func<string, Type> typeResolver)
         =>   documents
               .Where(d => d.GetPropertyValue<string>("type") == "Event")
               .Select(d => d.ToEventRead())
               .Select(d => d.ToEvent(typeResolver))
               .ToArray();
    }
}
