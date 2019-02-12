using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb.Order
{
	public class FooPolicy
	{
		public static ICommand When(IEvent @event) => new CreateOrderCommand(@event.AggregateId) { CorrelationId = @event.GetCorrelation() };
	}
}
