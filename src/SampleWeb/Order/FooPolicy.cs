using Fiffi;

namespace SampleWeb.Order
{
	public class FooPolicy
	{
		public static ICommand When(IEvent @event) => new CreateOrderCommand(@event.SourceId) { CorrelationId = @event.GetCorrelation() };
	}
}
