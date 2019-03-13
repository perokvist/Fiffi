using Fiffi.ServiceFabric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb.Cart
{
	public class CheckoutApplicationService
	{
		public static Task ExecuteAsync(ApplicationServiceContext context, CheckoutCommand command)
			=> context.ExecuteAsync<CartState>(command, state => new[] { new CartCheckedoutEvent(Guid.Parse(command.AggregateId.Id)) });
	}
}
