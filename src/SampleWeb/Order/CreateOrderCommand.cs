using Fiffi;
using System;

namespace SampleWeb.Order
{
	public class CreateOrderCommand : ICommand
	{
		public CreateOrderCommand(string aggregateId) => this.AggregateId = new AggregateId(aggregateId);

		public IAggregateId AggregateId { get; }

		public Guid CorrelationId { get; set; } = Guid.NewGuid();
	}
}
