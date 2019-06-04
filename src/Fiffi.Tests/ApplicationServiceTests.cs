using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class ApplicationServiceTests
    {

        [Fact]
        public async Task ReadWriteAsync()
        {
            var id = Guid.NewGuid();
            var store = new InMemoryEventStore();
            var streamName = typeof(TestState).Name.AsStreamName(id).StreamName;
            var arrangeVersion = await store.AppendToStreamAsync(streamName, 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId(id)) });

            await ApplicationService.ExecuteAsync<TestState>
                (store, new TestCommand(id), 
                state => new IEvent[] { new TestEvent {SourceId = id.ToString() } }, e => Task.CompletedTask);

            var result = await store.LoadEventStreamAsync(streamName, 0);

            Assert.Equal(1, arrangeVersion);
            Assert.Equal(2, result.Version);
        }

        public class TestEvent : IEvent
        {
            public string SourceId { get; set; }
            public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        }

        public class TestCommand : ICommand
        {
            private readonly Guid id;

            public TestCommand(Guid id)
            {
                this.id = id;
            }

            public IAggregateId AggregateId => new AggregateId(this.id);

            public Guid CorrelationId { get; set; } = Guid.NewGuid();
        }

        public class TestState
        {
            public TestState When(IEvent @event) => this;
        }
    }
}
