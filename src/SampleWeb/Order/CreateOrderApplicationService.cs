using Fiffi.ServiceFabric;
using System;
using System.Threading.Tasks;

namespace SampleWeb.Order
{
	public class CreateOrderApplicationService
	{
		public static Task ExecuteAsync(ApplicationServiceContext context, CreateOrderCommand command)
			=> context.ExecuteAsync<OrderState>(command, state => new[] { new OrderCreatedEvent(Guid.Parse(command.AggregateId.ToString())) });
	}
}
