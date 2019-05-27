using System;

namespace Fiffi.CosmoStore
{
    public static class Extensions
    {
        public static global::CosmoStore.EventWrite ToCosmosStoreEvent(this IEvent @event)
         => new global::CosmoStore.EventWrite(
                @event.Meta.GetEventMetaData().EventId,
                @event.Meta.GetEventMetaData().CorrelationId,
                Guid.NewGuid(),
                @event.Meta["type.name"], 
                Newtonsoft.Json.Linq.JToken.FromObject(@event),
                Newtonsoft.Json.Linq.JToken.FromObject(@event.Meta));
    }
}
