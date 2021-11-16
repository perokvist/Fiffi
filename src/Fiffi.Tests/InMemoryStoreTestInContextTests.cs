using Fiffi.InMemory;
using Fiffi.Testing;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests;

public class InMemoryStoreTestInContextTests
{
    ITestContext context;
    IStateStore stateStore;

    public InMemoryStoreTestInContextTests()
    => context = TestContextBuilder.Create<InMemoryStateStore>((store, q) =>
    {
        stateStore = store;
        return new TestContextForStateStore(a => a(store), c => store.ExecuteAsync<TestState>(c, s => Array.Empty<EventRecord>(), e => Task.CompletedTask), q, e => Task.CompletedTask);
    });

    [Fact]
    public async Task AppendToStreamAsync()
    {
        var id = new AggregateId(Guid.NewGuid());

        context.Given(new TestEvent(id).AddTestMetaData<TestState>(id));

        await context.WhenAsync(new TestCommand(id));
    }
}
