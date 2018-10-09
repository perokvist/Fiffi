﻿using Fiffi;
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

		readonly EventProcessor eventProcessor;

		public CartModule(Dispatcher<ICommand, Task> dispatcher, EventProcessor eventProcessor)
		{
			this.eventProcessor = eventProcessor;
			Dispatcher = dispatcher;
		}

		public Task DispatchAsync(ICommand command) => this.Dispatcher.Dispatch(command);

		public Task WhenAsync(IEvent @event) => this.eventProcessor.PublishAsync(@event);

		//public static CartModule Initialize() => Initialize(new InMemoryEventStore(), evts => Task.CompletedTask);

		public static CartModule Initialize(IReliableStateManager stateManager, Func<ITransaction, IEventStore> store, Func<IEvent[], Task> pub)
		{
			
			var commandDispatcher = new Dispatcher<ICommand, Task>();
			var policies = new EventProcessor();

			var publisher = new EventPublisher((tx, events) => stateManager.EnqueuAsync(tx, events), policies.PublishAsync, pub);
			var context = new ApplicationServiceContext(stateManager, store, publisher);

			commandDispatcher.Register<AddItemCommand>(cmd => AddItemApplicationService.Execute(context, cmd));

			return new CartModule(commandDispatcher, policies);
		}

	}
}
