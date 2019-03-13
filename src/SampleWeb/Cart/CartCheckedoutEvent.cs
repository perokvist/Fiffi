using Fiffi;
using System;
using System.Collections.Generic;

namespace SampleWeb.Cart
{
	public class CartCheckedoutEvent : IEvent
	{
		public CartCheckedoutEvent(Guid aggregateId) => this.AggregateId = aggregateId;

		public Guid AggregateId { get; }

		public IDictionary<string, string> Meta { get; set; }
	}
}
