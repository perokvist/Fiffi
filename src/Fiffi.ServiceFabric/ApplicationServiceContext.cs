using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public class ApplicationServiceContext
	{
		readonly IReliableStateManager stateManager;
		readonly Func<ITransaction, IEventStore> store;
		readonly Func<ITransaction, IEvent[], Task> pub;

		public ApplicationServiceContext(IReliableStateManager stateManager, Func<ITransaction, IEventStore> store, Func<ITransaction, IEvent[], Task> pub)
		{
			this.stateManager = stateManager;
			this.store = store;
			this.pub = pub;
		}

		public Task ExecuteAsync<TState>(ICommand command, Func<TState, IEvent[]> f)
			where TState : class, new()
			 => this.stateManager.UseTransactionAsync(
				tx => ApplicationService.ExecuteAsync(
					store(tx), command,
					f,
					events => pub(tx, events)
					)
				);
	}
}
