using Fiffi.InMemory;
using Fiffi.Testing;
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
			var command = new TestCommand(new AggregateId("test-id"));

			ep.Register<TestEvent>(e => Task.CompletedTask);

			await ApplicationService.ExecuteAsync<object>(new InMemoryEventStore(), command, state => new IEvent[] { new TestEvent(command.AggregateId.Id) }, ep.PublishAsync, locks);
		}

		[Fact]
		public async Task RegisterList()
		{
			var ep = new EventProcessor();
			bool multiCalled = false; ;
			var id = new AggregateId("test-id");

			Func<IEvent[], Task> multi = events => {
				multiCalled = true;
				return Task.CompletedTask;
			};

			ep.RegisterAll(multi);

			await ep.PublishAsync(new TestEvent(id).AddTestMetaData<string>(id), new TestEvent(id).AddTestMetaData<string>(id));

			Assert.True(multiCalled);
		
		}

		[Fact]
		public void Foo()
		{
			var registered = typeof(EventEnvelope<TestEventRecord>);
			var e = new EventEnvelope<EventRecord>("test", new TestEventRecord("testing")).AddTestMetaData("test").GetType();

			var published = typeof(EventEnvelope<EventRecord>);

			Assert.True(published.IsOrImplements(registered));
		}

		[Fact]
		public async Task FooAsync()
		{ 
			var ep = new EventProcessor();
			var called = false;
			ep.Register<IEvent<TestEventRecord>>(e =>
			{
				called = true;
				return Task.CompletedTask;
			});

			var e = EventEnvelope.Create("test", new TestEventRecord("testing")).AddTestMetaData("test") as IEvent;
			await ep.PublishAsync(e);

			Assert.True(called);
		}
	}
}
