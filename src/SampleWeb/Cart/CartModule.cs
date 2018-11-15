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
		readonly QueryDispatcher queryDispatcher;
		readonly Func<IEvent[], Task> publish;

		public CartModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher)
		{
			this.dispatcher = dispatcher;
			this.publish = publish;
			this.queryDispatcher = queryDispatcher;
		}

		public Task DispatchAsync(ICommand command) => this.dispatcher.Dispatch(command);

		public async Task<T> QueryAsync<T>(IQuery<T> q) => (T)await queryDispatcher.HandleAsync(q);

		public Task WhenAsync(IEvent @event) => publish(new[] { @event });

		public static CartModule Initialize(IReliableStateManager stateManager, Func<ITransaction, IEventStore> store, Func<IEvent[], Task> eventLogger)
		{
			var commandDispatcher = new Dispatcher<ICommand, Task>();
			var policies = new EventProcessor();
			var projections = new EventProcessor();
			var queryDispatcher = new QueryDispatcher();
			var queueSerializer = Serialization.Json();

			var publisher = new EventPublisher((tx, events) => stateManager.EnqueuAsync(tx, events, queueSerializer), eventLogger, projections.PublishAsync);
			var context = new ApplicationServiceContext(stateManager, store, publisher);

			commandDispatcher
				.WithContext(context)
				.Register<AddItemCommand>(AddItemApplicationService.ExecuteAsync);

			return new CartModule(commandDispatcher, policies.Merge(projections.PublishAsync), queryDispatcher);
		}

	}
}
