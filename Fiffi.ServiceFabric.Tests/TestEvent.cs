using System;
using System.Collections.Generic;

namespace Fiffi.ServiceFabric.Tests
{
	public class TestEvent : IEvent
	{
		public TestEvent(Guid id)
		{
			this.AggregateId = id;
			this.Meta["EventId"] = Guid.NewGuid().ToString();
		}

		public Guid AggregateId { get; set; }

		public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
	}
}