using Fiffi.Modularization;
using Fiffi.Testing;
using static Fiffi.AspNetCore.Testing.Tests.TestModule;

namespace Fiffi.AspNetCore.Testing.Tests;

public class TestInterfaceModule : IModule
{
    readonly IAdvancedEventStore store;
    readonly ISnapshotStore snapshotStore;
    readonly Func<IEvent[], Task> pub;

    public TestInterfaceModule(IAdvancedEventStore store, ISnapshotStore snapshotStore, Func<IEvent[], Task> pub)
    {
        this.store = store;
        this.snapshotStore = snapshotStore;
        this.pub = pub;
    }

    public Task DispatchAsync(ICommand command) =>
         store.ExecuteAsync(command, "test", () => new[] { new TestEventRecord("test") }, pub);

    public Task OnStart(IEvent[] events) => WhenAsync(events);

    public async Task<T?> QueryAsync<T>(IQuery<T> q) where T : class
     => await (q switch
     {
         TestQuery => snapshotStore.Get<T>("test"),
         _ => throw new InvalidOperationException()
     });

    public IAsyncEnumerable<T> QueryAsync<T>(IStreamQuery<T> q)
     => throw new NotImplementedException();

    public Task WhenAsync(params IEvent[] events) =>
        snapshotStore.Apply("test", new TestView(0), events.Select(x => x.Event), Apply);
}
