﻿using System.Text.Json.Serialization;

namespace Fiffi.Serialization;

//https://raw.githubusercontent.com/joseftw/JOS.SystemTextJsonDictionaryStringObjectJsonConverter/develop/src/JOS.SystemTextJsonDictionaryObjectModelBinder/DictionaryStringObjectJsonConverterCustomWrite.cs
public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
{
    public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
        }

        var dictionary = new Dictionary<string, object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("JsonTokenType was not PropertyName");
            }

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new JsonException("Failed to get property name");
            }

            reader.Read();

            dictionary.Add(propertyName, ExtractValue(ref reader, options));
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    //public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
    //{
    //    writer.WriteStartObject();

    //    foreach (var key in value.Keys)
    //    {
    //        HandleValue(writer, key, value[key]);
    //    }

    //    writer.WriteEndObject();
    //}

    //private static void HandleValue(Utf8JsonWriter writer, string key, object objectValue)
    //{
    //    if (key != null)
    //    {
    //        writer.WritePropertyName(key);
    //    }

    //    switch (objectValue)
    //    {
    //        case string stringValue:
    //            writer.WriteStringValue(stringValue);
    //            break;
    //        case DateTime dateTime:
    //            writer.WriteStringValue(dateTime);
    //            break;
    //        case long longValue:
    //            writer.WriteNumberValue(longValue);
    //            break;
    //        case int intValue:
    //            writer.WriteNumberValue(intValue);
    //            break;
    //        case float floatValue:
    //            writer.WriteNumberValue(floatValue);
    //            break;
    //        case double doubleValue:
    //            writer.WriteNumberValue(doubleValue);
    //            break;
    //        case decimal decimalValue:
    //            writer.WriteNumberValue(decimalValue);
    //            break;
    //        case bool boolValue:
    //            writer.WriteBooleanValue(boolValue);
    //            break;
    //        case Dictionary<string, object> dict:
    //            writer.WriteStartObject();
    //            foreach (var item in dict)
    //            {
    //                HandleValue(writer, item.Key, item.Value);
    //            }
    //            writer.WriteEndObject();
    //            break;
    //        case object[] array:
    //            writer.WriteStartArray();
    //            foreach (var item in array)
    //            {
    //                HandleValue(writer, item);
    //            }
    //            writer.WriteEndArray();
    //            break;
    //        default:
    //            writer.WriteNullValue();
    //            break;
    //    }
    //}

    //private static void HandleValue(Utf8JsonWriter writer, object value)
    //{
    //    HandleValue(writer, null, value);
    //}

    private object ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (reader.TryGetDateTime(out var date))
                {
                    return date;
                }
                return reader.GetString();
            case JsonTokenType.False:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                //if (reader.GetInt64().ToString().Length == 29)
                //return DateTime.UnixEpoch.AddSeconds(reader.GetInt64());
                if (reader.TryGetInt64(out var result))
                {
                    return result;
                }
                return reader.GetDecimal();
            case JsonTokenType.StartObject:
                return Read(ref reader, null, options);
            case JsonTokenType.StartArray:
                var list = new List<object>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    list.Add(ExtractValue(ref reader, options));
                }
                return list;
            default:
                throw new JsonException($"'{reader.TokenType}' is not supported");
        }
    }
}
