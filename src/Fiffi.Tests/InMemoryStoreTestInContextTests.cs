using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class InMemoryStoreTestInContextTests
    {
        ITestContext context;
        IStateStore stateStore;

        public InMemoryStoreTestInContextTests()
        => context = TestContextBuilder.Create<InMemoryStateStore>((store, q) =>
        {
            stateStore = store;
            return new TestContextForStateStore(a => a(store), c => ApplicationService.ExecuteAsync<TestState>(store, c, s => Array.Empty<IEvent>(), e => Task.CompletedTask)  , q, e => Task.CompletedTask);
        });

        [Fact]
        public async Task AppendToStreamAsync()
        {
            var id = new AggregateId(Guid.NewGuid());

            context.Given(new TestEvent(id).AddTestMetaData<TestState>(id));

            await context.WhenAsync(new TestCommand(id));
        }

        public class TestState
        {
            public TestState()
            {

            }
            public bool Called { get; set; }

            public TestState When(IEvent e) => this.Tap(x => x.Called = true);
        }

        public class TestEvent : IEvent
        {
            public TestEvent(IAggregateId id)
            {
                this.SourceId = id.ToString();
                this.Meta["eventid"] = Guid.NewGuid().ToString();
            }

            public string SourceId { get; set; }

            public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        }
    }
}
