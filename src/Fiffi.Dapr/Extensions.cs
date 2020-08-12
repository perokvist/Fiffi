using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Fiffi.Dapr
{
    public static class Extensions
    {
        public static IEnumerable<IEvent> FeedFilter(this IEnumerable<JsonDocument> docs, Func<string, Type> typeProvider)
            => docs
            .Where(x => !x.RootElement.GetProperty("id").GetString().EndsWith("|head"))
            .Where(x => x.RootElement.GetProperty("id").GetString().Contains("||gameaggregate"))
            .SelectMany(x => JsonSerializer.Deserialize<global::Dapr.EventStore.EventData[]>(x.RootElement.GetProperty("value").GetRawText()))
            .Select(x => (IEvent)JsonSerializer.Deserialize(x.Data, typeProvider(x.EventName)));
    }
}
