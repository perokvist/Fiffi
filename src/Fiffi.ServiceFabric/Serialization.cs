using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi.ServiceFabric
{
	public static class Serialization
	{
		public static Func<IEvent, EventData> FabricSerialization() => e => e.MapObject();

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


		public static Func<EventData, Type> JsonMetaAccessor(Func<string, Type> typeResolver) => ed =>
			JsonConvert.DeserializeObject<Dictionary<string, string>>(ed.Metadata.ToString()).GetEventType(typeResolver);

		public static Func<EventData, Type, IEvent> JsonDeserialization() => (ed, t) => 
			(IEvent)JsonConvert.DeserializeObject(ed.Body.ToString(), t);

		public static Func<EventData, IEvent> JsonDeserialization(Func<EventData, Type> metaAccessor, Func<EventData, Type, IEvent> deserializer) => ed =>
			deserializer(ed, metaAccessor(ed));

		public static Func<EventData, IEvent> JsonDeserialization(Func<string, Type> typeResolver) =>
			 JsonDeserialization(JsonMetaAccessor(typeResolver), JsonDeserialization());


		public static Func<EventData, IEvent> FabricDeserialization() => ed => ed.MapObject();

	}
}
