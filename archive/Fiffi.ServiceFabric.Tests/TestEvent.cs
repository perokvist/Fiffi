using System;
using System.Collections.Generic;

namespace Fiffi.ServiceFabric.Tests
{
	public class TestEvent : IEvent
	{
		public TestEvent(Guid id) : this(id.ToString())
		{ }


		public TestEvent(string id)
		{
			this.SourceId = id;
			this.Meta["eventid"] = Guid.NewGuid().ToString();
		}

		public string SourceId { get; set; }

		public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
	}
}