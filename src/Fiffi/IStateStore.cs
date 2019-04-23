using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public interface IStateStore
    {
        Task<(T State, long Version)> GetAsync<T>(IAggregateId id)
         where T : new();

        /// <remarks>Expects that state and outbox events is persisted in the same transaction</remarks>
        Task SaveAsync<T>(IAggregateId id, T state, long version, IEvent[] events)
         where T : new();

        Task<IEvent[]> GetOutBoxAsync(string sourceId);

        Task ClearOutBoxAsync(string sourceId, params Guid[] correlationIds);

        Task<IEvent[]> GetAllUnPublishedEventsAsync();
    }
}
