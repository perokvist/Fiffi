using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class CartModule
	{
		Dispatcher<ICommand, Task> Dispatcher { get; }

		public CartModule(Dispatcher<ICommand, Task> dispatcher)
		{
			Dispatcher = dispatcher;
		}

		public Task DispatchAsync(ICommand command) => this.Dispatcher.Dispatch(command);

		//public static CartModule Initialize() => Initialize(new InMemoryEventStore(), evts => Task.CompletedTask);

		public static CartModule Initialize(IReliableStateManager stateManager, IEventStore store, Func<IEvent[], Task> pub)
		{
			var commandDispatcher = new Dispatcher<ICommand, Task>();
			commandDispatcher.Register<AddItemCommand>(cmd => AddItemApplicationService.Execute(stateManager, tx => new ReliableEventStore(stateManager, tx), (tx, events) => stateManager.EnqueuAsync(tx, events), cmd));

			return new CartModule(commandDispatcher);

		}

	}
}
