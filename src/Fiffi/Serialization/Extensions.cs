using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi.Serialization
{
    public static class Extensions
    {
        public static IEvent Deserialize(this Func<string, Type> resolver, string json)
        {
            var meta = JsonSerializer.Deserialize<MetaEvent>(json);
            var e = (IEvent)JsonSerializer.Deserialize(json, resolver(meta.GetEventName()));
            if (e.Meta == null || !e.Meta.Any())
                e.Meta = meta.Meta;

            return e;
        }


        public static async Task<IEvent> DeserializeAsync(this Func<string, Type> resolver, Stream json)
        {
            var meta = await JsonSerializer.DeserializeAsync<MetaEvent>(json);
            var e = (IEvent)(await JsonSerializer.DeserializeAsync(json, resolver(meta.GetEventName())));
            if (e.Meta == null || !e.Meta.Any())
                e.Meta = meta.Meta;

            return e;
        }
    }

    public class MetaEvent : IEvent
    {
        public string SourceId { get; set; }
        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    }
}
