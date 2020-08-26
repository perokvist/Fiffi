using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using EventStore.Client;

namespace Fiffi.EventStoreDB
{
    public static class Extensions
    {
        public static IEvent ToEvent(this ResolvedEvent resolvedEvent, Func<string, Type> typeResolver)
        {
            var type = typeResolver(resolvedEvent.Event.EventType);
            var data = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());
            var @event = JsonSerializer.Deserialize(data, type) as IEvent;

            var meta = Encoding.UTF8.GetString(resolvedEvent.Event.Metadata.ToArray());
            @event.Meta = JsonSerializer.Deserialize<Dictionary<string, string>>(meta);
            return @event;
        }

        public static EventData ToEventData(this IEvent @event)
        {
            var metaByte = JsonSerializer.SerializeToUtf8Bytes(@event.Meta);
            @event.Meta = new Dictionary<string, string>();
            var dataByte = JsonSerializer.SerializeToUtf8Bytes(@event);
            var typeName = @event.GetEventName().ToCamelCase();

            return new EventData(Uuid.FromGuid(@event.EventId()), typeName, new ReadOnlyMemory<byte>(dataByte), new ReadOnlyMemory<byte>(metaByte));
        }

        private static string ToCamelCase(this string value)
            => char.ToLowerInvariant(value[0]) + value.Substring(1);
    }
}
