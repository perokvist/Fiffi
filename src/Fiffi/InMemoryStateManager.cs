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
			=> async events =>
			{
				var sourceId = events.First().SourceId;
				if (!events.All(x => x.SourceId == sourceId))
					throw new ArgumentException("All events are expected to have the same sourceId");

				await publish(events);

				var state = store[new AggregateId(sourceId)];
				events.ForEach(e => state.OutBox.Remove(e));
			};

		public async Task PublishAllUnPublishedEventsAsync(Func<IEvent[], Task> publish)
		{
			var eventsToPublish = store.Select(x => x.Value.OutBox.ToArray()).ToArray();
			await Task.WhenAll(eventsToPublish.Select(x => OnPublish(publish)(x))); //TODO force this
		}

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
