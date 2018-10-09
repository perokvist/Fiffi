using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public class ApplicationServiceContext
	{
		readonly IReliableStateManager stateManager;
		readonly Func<ITransaction, IEventStore> store;
		readonly EventPublisher eventPublisher;

		public ApplicationServiceContext(IReliableStateManager stateManager, Func<ITransaction, IEventStore> store, EventPublisher eventPublisher)
		{
			this.stateManager = stateManager;
			this.store = store;
			this.eventPublisher = eventPublisher;
		}

		public Task ExecuteAsync<TState>(ICommand command, Func<TState, IEvent[]> f, PublishMode publishMode = PublishMode.OutBoxQueue)
			where TState : class, new()
			 => this.stateManager.UseTransactionAsync(
				tx => ApplicationService.ExecuteAsync(
					store(tx), command,
					f,
					events => eventPublisher.PublishAsync(tx, publishMode, events)
					)
				);
	}
}
