using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public interface IStateStore
    {
        Task<(T State, long Version)> GetAsync<T>(IAggregateId id);

        /// <remarks>Expects that state and outbox events is persisted in the same transaction</remarks>
        Task SaveAsync<T>(IAggregateId aggregateId, T state, long version, IEvent[] outboxEvents);

        Task<IEvent[]> GetOutBoxAsync(string sourceId);

        Task ClearOutBoxAsync(string sourceId, params Guid[] correlationIds);

        Task<IEvent[]> GetAllUnPublishedEventsAsync();
    }
}
