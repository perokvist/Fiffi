using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi.ServiceFabric
{
	public static class MappingExtansions
	{
		//TODO custom event serialization ?
		static EventData MapObject(IEvent e) => new EventData(e.EventId(), e, e.Meta);

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

		static IEvent ToEvent(StorageEvent storageEvent) => (IEvent)storageEvent.EventBody;
	}
}
