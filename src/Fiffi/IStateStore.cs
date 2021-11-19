namespace Fiffi;

public interface IStateStore
{
    Task<(T State, long Version)> GetAsync<T>(IAggregateId id)
     where T : class, new();

    /// <remarks>Expects that state and outbox events is persisted in the same transaction</remarks>
    Task SaveAsync<T>(IAggregateId id, T state, long version, IEvent[] events)
     where T : class, new();

    Task CompleteOutBoxAsync(params IEvent[] events);

    Task<IEvent[]> GetAllUnPublishedEventsAsync();
}
