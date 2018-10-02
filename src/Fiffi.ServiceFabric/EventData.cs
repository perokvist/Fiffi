using System;

namespace Fiffi.ServiceFabric
{
	public class EventData
	{
		public Guid EventId { get; private set; }

		public object Body { get; private set; }

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
