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

		//TODO pass deserializtion and detect
		public static IEvent ToEvent(this StorageEvent storageEvent, Func<string, Type> typeResolver)
		{
			if (storageEvent.EventBody is string)
			{
				var meta = JsonConvert.DeserializeObject<Dictionary<string, string>>(storageEvent.Metadata.ToString());
				var t = meta.GetEventType(typeResolver);
				return (IEvent)JsonConvert.DeserializeObject(storageEvent.EventBody.ToString(), t);
			}
			else
			{
				return (IEvent)storageEvent.EventBody;
			}

		}

		public static EventData ToEventData(this StorageEvent storageEvent)
			=> new EventData(storageEvent.EventId, storageEvent.EventBody, storageEvent.Metadata);
	}
}
