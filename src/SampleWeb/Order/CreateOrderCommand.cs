using Fiffi;
using System;

namespace SampleWeb.Order
{
	public class CreateOrderCommand : ICommand
	{
		public CreateOrderCommand(Guid aggregateId) => this.AggregateId = new AggregateId(aggregateId.ToString());

		public IAggregateId AggregateId { get; }

		public Guid CorrelationId { get; set; } = Guid.NewGuid();
	}
}
