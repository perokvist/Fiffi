using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi
{
    public interface IEvent
    {
        EventRecord Event { get; }
        string SourceId { get; }
        IDictionary<string, string> Meta { get; set; }
    }

    public interface IEvent<T> : IEvent
    { 
         new T Event { get; set; }
    }

    public abstract record EventRecord;

    public static class EventEnvelope
    {
        public static EventEnvelope<T> Create<T>(string sourceId, T @event)
            where T : EventRecord
            => new EventEnvelope<T>(sourceId, @event);
    }

    public class EventEnvelope<T> : IEvent<T>
        where T : EventRecord
    {
        public EventEnvelope(string sourceId, T @event)
        {
            SourceId = sourceId;
            Event = @event;
        }

        public T Event { get; set; }
        public string SourceId { get; }
        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        EventRecord IEvent.Event => Event;

        public static implicit operator T (EventEnvelope<T> envelope) => envelope.Event;

    }

    public static class EventExtensions
    {
        public static IEvent[] ToEnvelopes(this EventRecord[] eventRecords, string sourceId)
            => eventRecords.Select(r => EventEnvelope.Create(sourceId, r)).ToArray();

        public static IEvent[] ToEnvelopes<T>(this T[] eventRecords, Func<T, string> sourceId)
            where T : EventRecord
           => eventRecords.Select(r => EventEnvelope.Create(sourceId(r), r)).ToArray();

        public static EventEnvelope<EventRecord>[] AsEnvelopes(this IEvent[] events)
            => events
            .Cast<EventEnvelope<EventRecord>>()
            .ToArray();

    }
}
