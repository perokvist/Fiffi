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
			var aggregateId = Guid.NewGuid();
			var dequeued = false;
			var events = new[] { new TestEvent(aggregateId), new TestEvent(aggregateId), new TestEvent(aggregateId) };
			await stateManager.EnqueuAsync(events);
			await stateManager.DequeueAsync<TestEvent>(e => {
				Assert.Equal(events.First(), e);
				dequeued = true;
				return Task.CompletedTask;
			}
			, CancellationToken.None);

			Assert.True(dequeued);
		}
	}
}
