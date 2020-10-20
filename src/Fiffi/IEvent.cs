using System.Collections.Generic;
using System.Linq;

namespace Fiffi
{
    public interface IEvent
	{
		string SourceId { get; }
		IDictionary<string, string> Meta { get; set; }
	}

    public abstract record Record;

    public abstract record EventRecord : Record;

    public static class EventEnvelope
    {
        public static EventEnvelope<T> Create<T>(string sourceId, T @event)
            where T : EventRecord
            => new EventEnvelope<T>(sourceId, @event);
    }

    public class EventEnvelope<T> : IEvent
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


    }

    public static class EventExtensions
    {
        public static IEvent[] ToEnvelopes(this EventRecord[] eventRecords, string sourceId)
            => eventRecords.Select(r => new EventEnvelope<EventRecord>(sourceId, r)).ToArray();

        public static EventEnvelope<EventRecord>[] AsEnvelopes(this IEvent[] events)
            => events
            .Cast<EventEnvelope<EventRecord>>()
            .ToArray();
      
    }
}
