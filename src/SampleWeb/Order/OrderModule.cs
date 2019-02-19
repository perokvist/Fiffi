using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using SampleWeb.Cart;
using System;
using System.Threading.Tasks;

namespace SampleWeb.Order
{
	public class OrderModule
	{
		readonly Dispatcher<ICommand, Task> dispatcher;
		readonly QueryDispatcher queryDispatcher;
		readonly Func<IEvent[], Task> publish;

		public OrderModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher)
		{
			this.dispatcher = dispatcher;
			this.publish = publish;
			this.queryDispatcher = queryDispatcher;
		}
		public Task DispatchAsync(ICommand command) => this.dispatcher.Dispatch(command);

		public Task WhenAsync(IEvent @event) => publish(new[] { @event });

		public static OrderModule Initialize(IReliableStateManager stateManager, Func<IEvent[], Task> eventLogger)
				=> Initialize(stateManager, tx => new ReliableEventStore(
				stateManager,
				tx,
				Serialization.Json(),
				Serialization.JsonDeserialization(TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(ItemAddedEvent)))) //TODO "share" with publisher
			),
			(tx, events) => stateManager.EnqueuAsync(tx, events, Serialization.Json()),
			eventLogger);


		public static OrderModule Initialize(IReliableStateManager stateManager, Func<ITransaction, IEventStore> store, Func<ITransaction, IEvent[], Task> outbox, Func<IEvent[], Task> eventLogger)
		{
			var commandDispatcher = new Dispatcher<ICommand, Task>();
			var policies = new EventProcessor();
			var projections = new EventProcessor();
			var queryDispatcher = new QueryDispatcher();
			var queueSerializer = Serialization.Json();

			var publisher = new EventPublisher(outbox, eventLogger, (tx, evts) => projections.PublishAsync(evts));
			var context = new ApplicationServiceContext(stateManager, store, publisher);

			commandDispatcher
				.WithContext(context)
				.Register<CreateOrderCommand>(CreateOrderApplicationService.ExecuteAsync);

			policies.RegisterReceptor<CartCheckedoutEvent>(commandDispatcher, FooPolicy.When);

			return new OrderModule(commandDispatcher, policies.Merge(projections.PublishAsync), queryDispatcher);
		}
	}
}
