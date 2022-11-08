using Fiffi.InMemory;
using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests;
public class SnapshotTests
{
    public record TestProjection(int Count);

    [Fact]
    public async Task ApplyLazyWithRecordAsync()
    {
        var snap = new InMemorySnapshotStore();
        var store = new InMemoryEventStore();
        var e = new[] { new TestEventRecord("test"), new TestEventRecord("test2") };
        var key = "testCache";

        await snap.ApplyLazy(store, e, key, "testStream", new TestProjection(0), 
            (state, e) => state with { Count = state.Count+1 }
        );

        var p = await snap.Get<TestProjection>(key);

        Assert.Equal(2, p.Count);
    }

    [Fact]
    public async Task ApplyWithDynamicWhenConvertionAsync()
    {
        var snap = new InMemorySnapshotStore();
        var store = new InMemoryEventStore();
        var e = new[] { new TestEventRecord("test"), new TestEventRecord("test2") }
            .Select((x, i) => EventEnvelope.Create(i.ToString(), x))
            .ToArray();
        var key = "testCache";

        await snap.Apply<TestState>(key, e);

        var p = await snap.Get<TestState>(key);

        Assert.Equal(2, p.Applied.Count);
    }
}
