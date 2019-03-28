using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Fiffi
{
	public class InMemoryStateManager : IStateManager
	{
		IDictionary<IAggregateId, (object State, IList<IEvent> OutBox)> store = new Dictionary<IAggregateId, (object, IList<IEvent>)>();

		public Task<T> GetAsync<T>(IAggregateId id)
			=> Task.FromResult((T)store[id].State);

		public Func<IEvent[], Task> OnPublish(Func<IEvent[], Task> publish)
			=> events =>
				Task.WhenAll(events
				.GroupBy(x => x.SourceId)
				.Select(async x =>
				{
					await publish(x.ToArray());
					var state = store[new AggregateId(x.Key)];
					events.ForEach(e => state.OutBox.Remove(e));
				}));

		public Task<IEvent[]> GetAllUnPublishedEvents()
		 => Task.FromResult(store.SelectMany(x => x.Value.OutBox).ToArray());

		public Task SaveAsync<T>(IAggregateId aggregateId, T state, IEvent[] outboxEvents)
		{
			if (store.ContainsKey(aggregateId))
				store[aggregateId] = (state, outboxEvents.ToList());
			else
				store.Add(aggregateId, (state, outboxEvents));

			return Task.CompletedTask;
		}
	}
}
