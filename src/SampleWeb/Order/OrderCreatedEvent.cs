using System;
using System.Collections.Generic;
using Fiffi;

namespace SampleWeb.Order
{
	public class OrderCreatedEvent : IEvent
	{
		public OrderCreatedEvent(string aggregateId) => this.SourceId = aggregateId;

		public string SourceId { get; }

		public IDictionary<string, string> Meta { get; set; }
	}
}