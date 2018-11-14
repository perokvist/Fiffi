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
		IEventStore store;
		IReliableStateManager stateManager;
		CartModule module;
		IEvent[] events;

		public TestContext()
		{
			this.stateManager = new MockReliableStateManager();
			//TODO fix double store
			this.store = new PublishingReliableEventStore(this.stateManager, Serialization.FabricSerialization(), Serialization.FabricDeserialization());
			this.module = CartModule.Initialize(stateManager, tx => new ReliableEventStore(stateManager, tx, Serialization.FabricSerialization(), Serialization.FabricDeserialization()), evts =>
			{
				events = evts;
				return Task.CompletedTask;
			});
		}

		public void Given(params IEvent[] events)
		{
			var streamName = $"{events.First().Meta["aggregatename"]}-{events.First().AggregateId}";
			store.AppendToStreamAsync(streamName, 0, events);
		}

		public Task When(ICommand command) => module.DispatchAsync(command);

		public void Then(Action<IEvent[]> f) => f(this.events);


	}
}
