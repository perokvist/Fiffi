namespace Fiffi;
public static partial class ApplicationService
{
    public static async Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, Task<EventRecord[]>> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
    where TState : class, new()
    => await aggregateLocks.UseLockAsync(command.AggregateId, command.CorrelationId, pub, async (publisher) =>
    await ExecuteAsync(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), action, publisher)
    );

    public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, EventRecord[]> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
        where TState : class, new()
        => ExecuteAsync<TState>(store, command, state => Task.FromResult(action(state)), pub, aggregateLocks);

}
