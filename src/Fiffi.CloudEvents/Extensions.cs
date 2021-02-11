using CloudNative.CloudEvents;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace Fiffi.CloudEvents
{
    public static class Extensions
    {
        public static CloudEvent ToCloudEvent(this IEvent @event, Uri? source = null)
            => new CloudEvent(CloudEventsSpecVersion.V1_0,
                @event.GetEventName(),
                source ?? new Uri($"urn:{@event.GetType().Namespace?.Replace('.', ':').ToLower()}"),
                @event.GetStreamName(),
                id: @event.SourceId,
                time: @event.OccuredAt(),
                new EventMetaDataExtension { MetaData = @event.Meta.GetEventMetaData() })
            {
                DataContentType = new ContentType(MediaTypeNames.Application.Json),
                Data = @event.Event
            };


        public static IEvent ToEvent(this CloudEvent cloudEvent, Func<string, Type> typeResolver)
        {
            var @event = cloudEvent.Data as EventRecord; 
            if (@event == null)
                throw new ArgumentException("expected cloud event data to by of type EventRecord");

            var envelope = EventEnvelope.Create(cloudEvent.Id, @event);
            envelope.Meta.AddMetaData(cloudEvent.Extension<EventMetaDataExtension>().MetaData);
            return envelope;
        }


        public static CloudEvent ToEvent(this string data)
            => new JsonEventFormatter().DecodeStructuredEvent(Encoding.UTF8.GetBytes(data), new EventMetaDataExtension());

        public static CloudEvent ToEvent(this IDictionary<string, object> data)
            => JsonSerializer.Serialize(data).ToEvent();

        public static string ToJson(this CloudEvent e)
            => Encoding.UTF8.GetString(new JsonEventFormatter().EncodeStructuredEvent(e, out _));

        public static IDictionary<string, object> ToMap(this string json)
            => JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
    }
}
