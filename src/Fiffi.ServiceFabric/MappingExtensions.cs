using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Fiffi.ServiceFabric
{
	public static class MappingExtensions
	{
		public static EventData MapObject(this IEvent e) => new EventData(e.EventId(), e, e.Meta);

		public static IEvent MapObject(this EventData e) => (IEvent)e.Body;

		public static Guid EventId(this IEvent e) => Guid.Parse(e.Meta["eventId"]);

		public static IEvent ToEvent(this EventData eventData, Func<EventData, IEvent> deserializer)
		{
			if (eventData.Body is string)
			{
				return deserializer(eventData);
			}
			return (IEvent)eventData.Body; //TODO not needed check
		}


		public static EventData ToEventData(this StorageEvent storageEvent)
			=> new EventData(storageEvent.EventId, storageEvent.EventBody, storageEvent.Metadata);
	}
}
