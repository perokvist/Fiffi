using CloudNative.CloudEvents;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
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
                Data = JsonSerializer.Serialize<object>(@event.Event)
            };

        public static IEvent ToEvent(this CloudEvent cloudEvent, Func<string, Type> typeResolver)
        {
            var eventType = typeResolver(cloudEvent.Type);
            var stringContent = cloudEvent.Data as string;
            if (stringContent == null)
                throw new ArgumentException("expected cloud event data to by of type string");

            var @event = JsonSerializer.Deserialize(stringContent, eventType) as EventRecord;
            var envelope = EventEnvelope.Create(cloudEvent.Id, @event);
            envelope.Meta.AddMetaData(cloudEvent.Extension<EventMetaDataExtension>().MetaData);
            return envelope;
        }
    }
}
