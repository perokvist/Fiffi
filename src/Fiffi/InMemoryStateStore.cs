using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;

namespace Fiffi
{
    public class InMemoryStateStore : IStateStore
    {
        IDictionary<IAggregateId, (object State, ISet<IEvent> OutBox)> store = new ConcurrentDictionary<IAggregateId, (object, ISet<IEvent>)>();

        public Task<T> GetAsync<T>(IAggregateId id)
            => Task.FromResult(store.ContainsKey(id) ? (T)store[id].State : default(T));

        public Task SaveAsync<T>(IAggregateId aggregateId, T state, IEvent[] outboxEvents)
        {
            if (store.ContainsKey(aggregateId))
                store[aggregateId] = (state, outboxEvents.ToHashSet());
            else
                store.Add(aggregateId, (state, outboxEvents.ToHashSet()));

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
