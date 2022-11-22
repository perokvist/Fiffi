using Fiffi.InMemory;
using Fiffi.Testing;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests;
public class ApplicationServiceTests
{
    [Fact]
    public async Task SnapshotUpdatedAsync()
    {
        var store = new InMemoryEventStore();
        var snap = new InMemorySnapshotStore();
        await store.ExecuteAsync(new TestCommand(new AggregateId("test")), "streamName",
            new TestState(), TestState.Apply, state => new[] { new TestEventRecord("test") } ,
            events => Task.CompletedTask, snap,
            state => state.Version, (v, state) => state.Tap(x => x.Version = v));

        var s = await snap.Get<TestState>($"streamName|snapshot");

        Assert.Equal(1, s.Version);
        Assert.Single(s.Applied);
    }

    [Fact]
    public async Task AppendAndLoadAsync()
    {
        var store = new InMemoryEventStore();
        await store.ExecuteAsync(new TestCommand(new AggregateId("test")), "streamName",
            new TestState(), TestState.Apply, state => new[] { new TestEventRecord("test") },
            events => Task.CompletedTask);

        var stream = await store.LoadEventStreamAsync("streamName", 0);

        var er = stream.Events.Select(x => x.Event).OfType<TestEventRecord>().First();
        var e = stream.Events.First();

        Assert.Equal("test", er.Message);
        Assert.Equal("streamName", e.Meta.GetEventMetaData().StreamName);
    }

    [Fact]
    public async Task ReadWriteAsync()
    {
        var id = Guid.NewGuid();
        var store = new InMemoryEventStore();
        var streamName = typeof(TestState).Name.AsStreamName(id).StreamName;
        var arrangeVersion = await store.AppendToStreamAsync(streamName, 0, new IEvent[] { new AggregateId(id).Pipe(x => new TestEvent(x).AddTestMetaData<string>(x)) });

        await ApplicationService.ExecuteAsync<TestState>
            (store, new TestCommand(id),
            state => new EventRecord[] { new TestEvent(id) }, e => Task.CompletedTask);

        var result = await store.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, arrangeVersion);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task VerionAppliedToSnapshot()
    {
        var id = Guid.NewGuid();
        var store = new InMemoryEventStore();
        var snapshotStore = new InMemorySnapshotStore();
        var streamName = typeof(TestState).Name.AsStreamName(id).StreamName;
        var arrangeVersion = await store.AppendToStreamAsync(streamName, 0, new IEvent[] { new AggregateId(id).Pipe(x => new TestEvent(x).AddTestMetaData<string>(x)) });

        await ApplicationService.ExecuteAsync<TestState>
           (store, new TestCommand(id),
           state => new EventRecord[] { new TestEventRecord("test") },
           e => Task.CompletedTask,
           snapshotStore, s => s.Version, (v, s) => s.Tap(x => x.Version = v));

        var result = await store.LoadEventStreamAsync(streamName, 0);
        var state = await snapshotStore.Get<TestState>($"{streamName}|snapshot");

        Assert.Equal(1, arrangeVersion);
        Assert.Equal(2, result.Version);
        Assert.Equal(2, state.Version);
        Assert.Equal(2, state.Applied.Count());
    }
}
