using System;

namespace Fiffi.CosmoStore
{
    public static class Extensions
    {
        public static global::CosmoStore.EventWrite ToCosmosStoreEvent(this IEvent @event)
         => new global::CosmoStore.EventWrite(
                @event.Meta.GetEventMetaData().EventId,
                @event.Meta.GetEventMetaData().CorrelationId,
                Guid.Empty,
                @event.Meta["type.name"], 
                Newtonsoft.Json.Linq.JToken.FromObject(@event),
                Newtonsoft.Json.Linq.JToken.FromObject(@event.Meta));

        public static IEvent ToEvent(this global::CosmoStore.EventRead eventRead, Func<string, Type> typeResolver)
         => typeResolver(eventRead.Name).Pipe(t => (IEvent)eventRead.Data.ToObject(t));
    }
}
