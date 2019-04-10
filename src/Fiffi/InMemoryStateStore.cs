using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using System.Data;

namespace Fiffi
{
    public class InMemoryStateStore : IStateStore
    {
        IDictionary<IAggregateId, ((object Value, long Version) State, ISet<IEvent> OutBox)> store = new ConcurrentDictionary<IAggregateId, ((object Value, long Version) State, ISet<IEvent> OutBox)>();

        public Task<(T State, long Version)> GetAsync<T>(IAggregateId id)
            => Task.FromResult<(T State, long Version)>(store.ContainsKey(id) ? ((T)store[id].State.Value, store[id].State.Version) : (default(T), default(long)));

        public Task SaveAsync<T>(IAggregateId aggregateId, T state, long version, IEvent[] outboxEvents)
        {
            if (store.ContainsKey(aggregateId))
            {
                if (store[aggregateId].State.Version != version)
                    throw new DBConcurrencyException($"wrong version - expected {version} but was {store[aggregateId].State.Version}");

                store[aggregateId] = ((state, version + 1), outboxEvents.ToHashSet());
            }
            else
                store.Add(aggregateId, ((state, version + 1), outboxEvents.ToHashSet()));

            return Task.CompletedTask;
        }

        public Task<IEvent[]> GetOutBoxAsync(string sourceId)
            => new AggregateId(sourceId)
                .Pipe(id => Task.FromResult(store.ContainsKey(id) ? store[id].OutBox.ToArray() : Array.Empty<IEvent>()));

        public Task ClearOutBoxAsync(string sourceId, params Guid[] correlationIds)
            => new AggregateId(sourceId)
                .Tap(id => store.DoIf(s => s.ContainsKey(id), s =>
                    s[id].OutBox
                    .Where(x => correlationIds.Any(c => c == x.GetCorrelation()))
                    .ToList()
                    .ForEach(e => s[id].OutBox.Remove(e))
                ))
                .Pipe(id => Task.CompletedTask);

        public Task<IEvent[]> GetAllUnPublishedEventsAsync()
             => Task.FromResult(store.SelectMany(x => x.Value.OutBox).ToArray());

    }
}
