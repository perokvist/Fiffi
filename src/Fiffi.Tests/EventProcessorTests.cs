using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
	public class EventProcessorTests
	{
		[Fact]
		public async Task EventProcessorReleasesLockAsync()
		{
			var locks = new AggregateLocks();
			var ep = new EventProcessor(locks);
			var command = new TestCommand { AggregateId = new AggregateId("test-id") };

			ep.Register<TestEvent>(e => Task.CompletedTask);

			await ApplicationService.ExecuteAsync<object>(new InMemoryEventStore(), command, state => new IEvent[] { new TestEvent() { SourceId = command.AggregateId.Id } }, ep.PublishAsync, locks);
		}

		public class TestCommand : ICommand
		{
			public IAggregateId AggregateId { get; set; }

			public Guid CorrelationId { get; set; } = Guid.NewGuid();
		}

		public class TestEvent : IEvent
		{
			public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

			public string SourceId { get; set; }
		}
	}
}
