using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class CartState
	{ }

	public class AddItemCommand : ICommand
	{
		public AddItemCommand(Guid aggregateId)
		{
			this.AggregateId = new AggregateId(aggregateId.ToString());
		}

		public Guid ItemId { get; set; }

		public Guid CorrelationId { get; } = Guid.NewGuid();

		public IAggregateId AggregateId { get; private set; }
	}

	[DataContract]
	public class ItemAddedEvent : IEvent
	{
		public ItemAddedEvent(IAggregateId aggregateId)
		{
			this.AggregateId = Guid.Parse(aggregateId.ToString());
		}

		[DataMember]
		public Guid AggregateId { get; set; }

		[DataMember]
		public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
	}
}
