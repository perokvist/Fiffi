using System;
using Xunit;
using ServiceFabric.Mocks;
using System.Threading.Tasks;
using System.Data;
using System.Linq;

namespace Fiffi.ServiceFabric.Tests
{
	public class EventStoreTests
	{
		[Fact]
		public async Task IncreaseVersionWithSingleEvent()
		{
			var stateManager = new MockReliableStateManager();
			var newVersion = await stateManager.AppendToStreamAsync("testStream", 0, new[] { new TestEvent(Guid.NewGuid()) });

			Assert.Equal(1, newVersion);
		}

		[Fact]
		public async Task IncreaseVersionWithMultipleEvents()
		{
			var stateManager = new MockReliableStateManager();
			var newVersion = await stateManager.AppendToStreamAsync("testStream", 0, new[] { new TestEvent(Guid.NewGuid()), new TestEvent(Guid.NewGuid()) });

			Assert.Equal(2, newVersion);
		}

		[Fact]
		public async Task IncreaseVersionWithDifferentStreams()
		{
			var stateManager = new MockReliableStateManager();
			var newVersion = await stateManager.AppendToStreamAsync("testStream", 0, new[] { new TestEvent(Guid.NewGuid()) });
			var newVersion2 = await stateManager.AppendToStreamAsync("testStream2", 0, new[] { new TestEvent(Guid.NewGuid()), new TestEvent(Guid.NewGuid()) });

			Assert.Equal(1, newVersion);
			Assert.Equal(2, newVersion2);
		}

		[Fact]
		public async Task IncreaseVersionWithPriorEvents()
		{
			var stateManager = new MockReliableStateManager();
			var newVersion = await stateManager.AppendToStreamAsync("testStream", 0, new[] { new TestEvent(Guid.NewGuid()) });
			var newVersion2 = await stateManager.AppendToStreamAsync("testStream", 1, new[] { new TestEvent(Guid.NewGuid()) });

			Assert.Equal(2, newVersion2);
		}

		[Fact]
		public async Task WrongVersionWithPriorEventsThrows()
		{
			var stateManager = new MockReliableStateManager();
			await stateManager.AppendToStreamAsync("testStream", 0, new[] { new TestEvent(Guid.NewGuid()) });

			await Assert.ThrowsAsync<DBConcurrencyException>(() => stateManager.AppendToStreamAsync("testStream", 0, new[] { new TestEvent(Guid.NewGuid()) }));
		}

		//TODO duplicate event detection

		[Fact]
		public async Task LoadEventsFormStreamWithEvents()
		{
			var stateManager = new MockReliableStateManager();
			var aggregateId = Guid.NewGuid();
			var @event = new TestEvent(aggregateId);
			await stateManager.AppendToStreamAsync("testStream", 0, new[] { @event });

			var events = await stateManager.LoadEventStreamAsync("testStream", 0, TypeResolver.Default());

			Assert.Equal(@event, events.Item1.First());
		}

		[Fact]
		public async Task LoadEventsFromVersionFormStreamWithEvents()
		{
			var stateManager = new MockReliableStateManager();
			var aggregateId = Guid.NewGuid();
			var events = new[] { new TestEvent(aggregateId), new TestEvent(aggregateId), new TestEvent(aggregateId) };
			await stateManager.AppendToStreamAsync("testStream", 0, events);

			var loadedEvents = await stateManager.LoadEventStreamAsync("testStream", 2, TypeResolver.Default());

			Assert.Equal(events.Last(), loadedEvents.Item1.First());
		}
	}
}
