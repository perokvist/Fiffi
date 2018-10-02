using System;

namespace Fiffi.ServiceFabric
{
	public class StorageEvent
	{
		public string StreamId { get; private set; }

		public object EventBody { get; private set; }

		public object Metadata { get; private set; }

		public int EventNumber { get; private set; }

		public Guid EventId { get; private set; }

		public StorageEvent(string streamId, EventData data, int eventNumber)
		{
			StreamId = streamId;
			EventBody = data.Body;
			Metadata = data.Metadata;
			EventNumber = eventNumber;
			EventId = data.EventId;
		}
	}
}
