using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests;

public class AggregateLockTests
{
    [Fact]
    public async Task OneActionPerInstanceIdAsync()
    {
        var locks = new AggregateLocks();
        var command = new TestCommand(new AggregateId("test-id"));
        var command2 = new TestCommand(new AggregateId("test-id"));

        var state = 0;
        var count = 0;

        await Task.WhenAll(
            locks.UseLockAsync(command.AggregateId, command.CorrelationId, events => Task.CompletedTask, f =>
            {
                state = 1;
                count++;
                return Task.Delay(100);
            }),
            locks.UseLockAsync(command2.AggregateId, command2.CorrelationId, events => Task.CompletedTask, f =>
            {
                state = state == 1 ? state = 2 : state;
                count++;
                return Task.CompletedTask;
            }));

        Assert.Equal(2, state);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task PublishesEvents()
    {
        var locks = new AggregateLocks();
        var command = new TestCommand(new AggregateId("test-id"));

        var pub = false;

        await locks.UseLockAsync(command.AggregateId, command.CorrelationId, events =>
        {
            pub = events.Any();
            return Task.CompletedTask;
        }, async publisher =>
        {
            locks.ReleaseIfPresent((command.AggregateId, command.CorrelationId));
            await publisher(new IEvent[] { new TestEvent(command.AggregateId) });
        });

        Assert.True(pub);
    }

    [Fact]
    public async Task ReleasesIfNoEvents()
    {
        var locks = new AggregateLocks();
        var command = new TestCommand(new AggregateId("test-id"));

        await locks.UseLockAsync(command.AggregateId, command.CorrelationId, events => Task.CompletedTask,
        publisher => publisher(Array.Empty<IEvent>()));
    }

    [Fact]
    public async Task ThrowsWhenTimedout()
    {
        var locks = new AggregateLocks();
        var command = new TestCommand("test-id");

        await Assert.ThrowsAsync<TimeoutException>(() =>
            locks.UseLockAsync(command.AggregateId, command.CorrelationId, events => Task.CompletedTask,
            publisher => publisher(new IEvent[] { new TestEvent("test-id") }))
        );
    }

}
