using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi.ServiceFabric
{
	public static class MappingExtensions
	{
		//TODO custom event serialization ?
		public static EventData MapObject(this IEvent e) => new EventData(e.EventId(), e, e.Meta);

		static EventData MapJson(IEvent e)
			=> new EventData(e.EventId(),
			JsonConvert.SerializeObject(e, new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				TypeNameHandling = TypeNameHandling.Auto
			}),
			JsonConvert.SerializeObject(e.Meta, new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				TypeNameHandling = TypeNameHandling.None
			}));

		static Guid EventId(this IEvent e) => Guid.Parse(e.Meta["eventId"]);

		public static IEvent ToEvent(this StorageEvent storageEvent) => (IEvent)storageEvent.EventBody;
	}
}
