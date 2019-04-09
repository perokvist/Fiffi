using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class TestContextForStateStoreTests
    {
        ITestContext context;
        IStateStore stateStore;

        public TestContextForStateStoreTests()
        => context = TestContextBuilder.Create<InMemoryStateStrore>((store, q) =>
        {
            stateStore = store;
            return new TestContextForStateStore(a => a(store), c => Task.CompletedTask, q, e => Task.CompletedTask);
        });


        [Fact]
        public async Task ContextCreatesStateAsync()
        {
            var id = new AggregateId(Guid.NewGuid());

            context.Given(new TestEvent(id).AddTestMetaData<TestState>(id));

            await context.WhenAsync(new TestCommand(id));

            var state = await stateStore.GetAsync<TestState>(id);

            Assert.True(state.Called);
        }

        public class TestState
        {
            public TestState()
            {

            }
            public bool Called { get; set; }

            public void When(IEvent e) => Called = true;
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
