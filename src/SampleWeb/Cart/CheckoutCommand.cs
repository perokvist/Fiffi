using System;
using Fiffi;

namespace SampleWeb
{
	public class CheckoutCommand : ICommand
	{
		public CheckoutCommand(Guid aggregate) => this.AggregateId = new AggregateId(aggregate.ToString());

		public IAggregateId AggregateId { get;  }

		public Guid CorrelationId { get; set; } = Guid.NewGuid();
	}
}