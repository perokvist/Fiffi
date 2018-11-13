using System;
using System.Runtime.Serialization;

namespace Fiffi.ServiceFabric
{
	[DataContract]
	public class EventData
	{
		[DataMember]
		public Guid EventId { get; private set; }

		[DataMember]
		public object Body { get; private set; }

		[DataMember]
		public object Metadata { get; private set; }

		public EventData(Guid eventId, object body, object metadata = null)
		{
			//Guard.IsNotNull(nameof(body), body);

			EventId = eventId;
			Body = body;
			Metadata = metadata;
		}
	}
}
