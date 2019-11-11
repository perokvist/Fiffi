using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using ServiceFabric.Mocks;
using System.Threading;
using System.Linq;

namespace Fiffi.ServiceFabric.Tests
{
	public class QueueTests
	{

		[Fact]
		public async Task ReadAndWriteFromEventQueue()
		{
			var stateManager = new MockReliableStateManager();
			var aggregateId = Guid.NewGuid().ToString();
			var dequeued = false;
			var events = new[] { new TestEvent(aggregateId), new TestEvent(aggregateId), new TestEvent(aggregateId) };
			await stateManager.EnqueuAsync(events, Serialization.FabricSerialization());
			await stateManager.DequeueAsync(e => {
				Assert.Equal(events.First(), e);
				dequeued = true;
				return Task.CompletedTask;
			}, Serialization.FabricDeserialization()
			, CancellationToken.None);

			Assert.True(dequeued);
		}
	}
}
