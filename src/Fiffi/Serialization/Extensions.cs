using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text.Json;

namespace Fiffi.Serialization;

public static class Extensions
{
    public static Dictionary<string, object> ToMap(this object item, JsonSerializerOptions opt = null)
        => JsonSerializer.Serialize(item, opt).ToMap(opt);

    public static Dictionary<string, object> ToMap(this string json, JsonSerializerOptions opt = null)
        => JsonSerializer.Deserialize<Dictionary<string, object>>(json, opt) ?? new();

    public static Func<IEvent, JsonSerializerOptions, Dictionary<string, object>> AsMap() => (e, opt)
        => JsonSerializer.Serialize<object>(e, opt).ToMap(opt);

    public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
    {
        var bufferWriter = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(bufferWriter))
            element.WriteTo(writer);
        var result = JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options);
        return result;
    }

    public static T ToObject<T>(this IDictionary<string, object> d, JsonSerializerOptions options = null)
    {
        var json = JsonSerializer.Serialize(d, options);
        var result = JsonSerializer.Deserialize<T>(json, options);
        return result;
    }
}

public record MetaEvent : IEvent
{
    public MetaEvent(string sourceId) => (SourceId, Meta) = (sourceId, new Dictionary<string, string>());

    public string SourceId { get; init; }

    public IDictionary<string, string> Meta { get; set; }

    public EventRecord Event => throw new NotImplementedException();
}
