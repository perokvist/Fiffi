using Fiffi;
using Microsoft.ServiceFabric.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ServiceFabric.Mocks;
using Fiffi.ServiceFabric;

namespace SampleWeb.Tests
{
	public class CartTests
	{
		[Fact]
		public async Task AddItemToCartAsync()
		{
			var context = new TestContext();

			//Given


			//When
			await context.When(new AddItemCommand(Guid.NewGuid()));

			//Then
			context.Then(events => Assert.True(events.OfType<ItemAddedEvent>().Count() == 1));
		}
	}

	public class TestContext
	{
		IReliableStateManager stateManager;
		CartModule module;
		IEvent[] events;
		Func<ITransaction, IEventStore> factory;

		public TestContext()
		{
			this.stateManager = new MockReliableStateManager();
			this.factory = tx => new ReliableEventStore(stateManager, tx, Serialization.FabricSerialization(), Serialization.FabricDeserialization());
			this.module = CartModule.Initialize(stateManager, factory, evts =>
			{
				events = evts;
				return Task.CompletedTask;
			});
		}

		public void Given(params IEvent[] events)
		{
			var streamName = $"{events.First().Meta["aggregatename"]}-{events.First().AggregateId}";
			UseStore(store => store.AppendToStreamAsync(streamName, 0, events));
		}

		public Task When(ICommand command) => module.DispatchAsync(command);

		public void Then(Action<IEvent[]> f) => f(this.events);
			
		void UseStore(Func<IEventStore, Task> f) => this.stateManager.UseTransactionAsync(tx => f(factory(tx))).GetAwaiter().GetResult();
	}
}
