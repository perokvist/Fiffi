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
		readonly Dispatcher<ICommand, Task> dispatcher;
		readonly EventProcessor eventProcessor;
		readonly QueryDispatcher queryDispatcher;

		public CartModule(Dispatcher<ICommand, Task> dispatcher, EventProcessor eventProcessor, QueryDispatcher queryDispatcher)
		{
			this.queryDispatcher = queryDispatcher;
			this.eventProcessor = eventProcessor;
			this.dispatcher = dispatcher;
		}

		public Task DispatchAsync(ICommand command) => this.dispatcher.Dispatch(command);

		public async Task<T> QueryAsync<T>(IQuery<T> q)	=> (T)await queryDispatcher.HandleAsync(q);

		public Task WhenAsync(IEvent @event) => this.eventProcessor.PublishAsync(@event);

		public static CartModule Initialize(IReliableStateManager stateManager, Func<ITransaction, IEventStore> store, Func<IEvent[], Task> spy)
		{
			var commandDispatcher = new Dispatcher<ICommand, Task>();
			var policies = new EventProcessor();
			var projections = new EventProcessor();
			var queryDispatcher = new QueryDispatcher();
			var queueSerializer = Serialization.Json();

			//TODO join projections and polics publish

			var publisher = new EventPublisher((tx, events) => stateManager.EnqueuAsync(tx, events, queueSerializer), spy, projections.PublishAsync);
			var context = new ApplicationServiceContext(stateManager, store, publisher);

			commandDispatcher.Register<AddItemCommand>(cmd => AddItemApplicationService.ExecuteAsync(context, cmd));

			return new CartModule(commandDispatcher, policies, queryDispatcher);
		}

	}
}
