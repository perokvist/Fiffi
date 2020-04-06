﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

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
}
