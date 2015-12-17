using System;

namespace Fiffi
{
	public class EventData
	{
		public Guid EventId { get;}
		public string Type { get; }
		public bool IsJson { get; }
		public byte[] Data { get; }
		public byte[] Metadata { get; }

		public EventData(Guid eventId, string type, bool isJson, byte[] data, byte[] metadata)
		{
			EventId = eventId;
			Type = type;
			IsJson = isJson;
			Data = data;
			Metadata = metadata;
		}
	}
}