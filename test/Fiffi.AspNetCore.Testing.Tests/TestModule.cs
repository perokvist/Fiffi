using Fiffi.Modularization;
using Fiffi.Testing;

namespace Fiffi.AspNetCore.Testing.Tests;

public class TestModule : Module
{
    public TestModule(ModuleCore core) : base(core)
    { }

    public static TestModule Initialize(IAdvancedEventStore store, ISnapshotStore snapshotStore, Func<IEvent[], Task> pub)
        => new Configuration<TestModule>(x => new TestModule(x))
        .Commands(cmd => store.ExecuteAsync(cmd, "test", () => new[] { new TestEventRecord("test") }, pub))
        .Updates(events => snapshotStore.Apply("test", new TestView(0), events.Select(x => x.Event), Apply))
        .Triggers((events, dispatcher) => Task.CompletedTask)
        .Query<TestQuery, TestView?>(q => snapshotStore.Get<TestView>("test"))
        .Create(store);

    public record TestView(int EventCount) : View;

    public static TestView Apply(
        TestView snap,
        EventRecord @event) => snap with { EventCount = snap.EventCount + 1 };

    public record TestQuery() : IQuery<TestView?>;
}