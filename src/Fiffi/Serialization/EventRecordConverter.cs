using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiffi.Serialization
{
    public class EventRecordConverter : JsonConverter<EventRecord>
    {
        public override EventRecord Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException($"{nameof(EventRecordConverter)} doesn't support read");
        }

        public override void Write(Utf8JsonWriter writer, EventRecord value, JsonSerializerOptions options)
            => JsonSerializer.Serialize<object>(writer, value, options);
    }
}
