using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fiffi.Serialization
{
    public static class Extensions
    {

        public static IDictionary<string, object> ToMap(this string json, JsonSerializerOptions opt = null)
            => JsonSerializer.Deserialize<Dictionary<string, object>>(json, opt) ?? new();

        public static Func<IEvent, JsonSerializerOptions, object> AsMap() => (e, opt)
            => JsonSerializer.Serialize(e, opt).ToMap(opt);

        //public static IEvent Deserialize(this Func<string, Type> resolver, string json)
        //{
        //    var meta = JsonSerializer.Deserialize<MetaEvent>(json);
        //    var e = (IEvent)JsonSerializer.Deserialize(json, resolver(meta.GetEventName()));
        //    if (e.Meta == null || !e.Meta.Any())
        //        e.Meta = meta.Meta;

        //    return e;
        //}


        //public static async Task<IEvent> DeserializeAsync(this Func<string, Type> resolver, Stream json)
        //{
        //    var meta = await JsonSerializer.DeserializeAsync<MetaEvent>(json);
        //    var e = (IEvent)(await JsonSerializer.DeserializeAsync(json, resolver(meta.GetEventName())));
        //    if (e.Meta == null || !e.Meta.Any())
        //        e.Meta = meta.Meta;

        //    return e;
        //}
    }

    public record MetaEvent : IEvent
    {
        public MetaEvent(string sourceId) => (SourceId, Meta) = (sourceId, new Dictionary<string, string>());

        public string SourceId { get; init; }

        public IDictionary<string, string> Meta { get; set; }

        public EventRecord Event => throw new NotImplementedException();
    }
}
