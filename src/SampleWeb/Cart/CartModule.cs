using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Data;
using SampleWeb.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Fiffi.ApplicationService;

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

		public static CartModule Initialize(IReliableStateManager stateManager, MailboxOptions mailboxOptions, Func<IEvent[], Task> eventLogger)
		 => Initialize(stateManager, tx => new ReliableEventStore(
						stateManager,
						tx,
						Serialization.Json(),
						Serialization.JsonDeserialization(TypeResolver.Default())
					),
			 Outbox.Writer(stateManager, mailboxOptions.Serializer),
			 eventLogger);


		public static CartModule Initialize(
			IReliableStateManager stateManager,
			Func<ITransaction, IEventStore> store,
			Func<ITransaction, IEvent[], Task> outbox,
			Func<IEvent[], Task> eventLogger)
		{
			var commandDispatcher = new Dispatcher<ICommand, Task>();
			var policies = new EventProcessor();
			var projections = new EventProcessor();
			var queryDispatcher = new QueryDispatcher();

			var publisher = new EventPublisher(outbox, eventLogger, (tx, evts) => projections.PublishAsync(evts));
			var context = new ApplicationServiceContext(stateManager, store, publisher);

			//IStateFoo stateFoo = null;
			//commandDispatcher.Register<AddItemCommand>(cmd =>
			//	ApplicationService.ExecuteAsync<CartState>(() => stateFoo.Get<CartState>(cmd.AggregateId), (state, evts) => stateFoo.Save(state,evts), cmd, state => Array.Empty<IEvent>() ,stateFoo.OnPublish(evts => Task.CompletedTask))
			//);

			commandDispatcher
				.WithContext(context)
				.Register<AddItemCommand>(AddItemApplicationService.ExecuteAsync)
				.Register<CheckoutCommand>(CheckoutApplicationService.ExecuteAsync);

			return new CartModule(commandDispatcher, policies.Merge(projections.PublishAsync), queryDispatcher);
		}

	}
	public static class CartModuleExtensions
	{
		public static IServiceCollection AddCart(this IServiceCollection services)
		=> services.AddSingleton(sc => CartModule.Initialize(sc.GetRequiredService<IReliableStateManager>(), sc.GetRequiredService<IOptions<MailboxOptions>>().Value, events => Task.CompletedTask)); //TODO global eventlogger


	}
}
