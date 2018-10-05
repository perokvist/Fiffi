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
				tx => ApplicationService.ExecuteAsync(
					store(tx), command,
					Execute(command),
					events => pub(tx, events)
					)
				);

		public static Func<CartState, IEvent[]> Execute(AddItemCommand command)
			=> state =>  new[] { new ItemAddedEvent(command.AggregateId) };


	}
}
