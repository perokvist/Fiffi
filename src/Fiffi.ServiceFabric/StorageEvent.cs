using System;
using System.Runtime.Serialization;

namespace Fiffi.ServiceFabric
{
	[DataContract]
	public class StorageEvent
	{
		[DataMember]
		public string StreamId { get; private set; }

		[DataMember]
		public object EventBody { get; private set; }

		[DataMember]
		public object Metadata { get; private set; }

		[DataMember]
		public int EventNumber { get; private set; }

		[DataMember]
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
