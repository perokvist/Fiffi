using System;
using System.Collections.Generic;
using Fiffi;

namespace SampleWeb.Order
{
	public class OrderCreatedEvent : IEvent
	{
		public OrderCreatedEvent(Guid aggregateId) => this.AggregateId = aggregateId;

		public Guid AggregateId { get; }

		public IDictionary<string, string> Meta { get; set; }
	}
}