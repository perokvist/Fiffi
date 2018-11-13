using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi.ServiceFabric
{
	public static class Serialization
	{
		public static Func<IEvent, EventData> ObjectSerialization() => e => e.MapObject();

		public static Func<IEvent, EventData> Json() => e => e.ToJson();

		public static EventData ToJson(this IEvent e)
		=> new EventData(e.EventId(),
		JsonConvert.SerializeObject(e, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			TypeNameHandling = TypeNameHandling.None
		}),
		JsonConvert.SerializeObject(e.Meta, new JsonSerializerSettings
		{
			Formatting = Formatting.Indented,
			TypeNameHandling = TypeNameHandling.None
		}));

		public static Func<EventData, IEvent> ObjectDeserialization() => e => e.MapObject();

	}
}
