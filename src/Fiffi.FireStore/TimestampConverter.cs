using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fiffi.FireStore;

public class JsonTimestampConverter : JsonConverter<Google.Cloud.Firestore.Timestamp>
{
    public override Timestamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Timestamp value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToDateTime());
}
