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
		public static Task Execute(IReliableStateManager stateManager, Func<ITransaction, IEventStore> store, Func<ITransaction, IEvent[], Task> pub, AddItemCommand command)
			=> stateManager.UseTransactionAsync(
				tx => ApplicationService.ExecuteAsync<CartState>(
					store(tx), command,
					state => new[] { new ItemAddedEvent() },
					events => pub(tx, events)
					)
				);


	}
}
