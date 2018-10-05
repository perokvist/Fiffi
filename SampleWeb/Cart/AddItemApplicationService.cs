using Fiffi;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fiffi.ServiceFabric;

namespace SampleWeb
{
	public static class AddItemApplicationService
	{
		public static Task Execute(ApplicationServiceContext context, AddItemCommand command)
		=> context.ExecuteAsync<CartState>(command, state => new[] { new ItemAddedEvent(command.AggregateId) });
	}
}
