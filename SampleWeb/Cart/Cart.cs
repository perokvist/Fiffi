using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class CartState
	{ }

	public class AddItemCommand : ICommand
	{
		public Guid AggregateId { get; set; } = Guid.NewGuid();
	}

	public class ItemAddedEvent : IEvent
	{
		public ItemAddedEvent(Guid aggregateId)
		{
			this.AggregateId = aggregateId;
		}
		public Guid AggregateId { get;  }

		public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
	}
}
