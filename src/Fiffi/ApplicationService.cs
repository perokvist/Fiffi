using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class ApplicationService
	{
		public static Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub)
			where TState : class, new()
			=> ExecuteAsync<TState>(store, command, state => Task.FromResult(action(state)) , pub);

		public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
			where TState : class, new()
		{
			if (command.CorrelationId == default(Guid))
				throw new ArgumentException("CorrelationId required");

			var aggregateName = typeof(TState).Name.Replace("State", "Aggregate").ToLower();
			var streamName = $"{aggregateName}-{command.AggregateId}";
			var happend = await store.LoadEventStreamAsync(streamName, 0);
			var state = happend.Events.Rehydrate<TState>();
			var events = await action(state);

			events
				.Where(x => x.Meta == null)
				.ForEach(x => x.Meta = new Dictionary<string, string>());

			events
				.ForEach(x => x
						.Tap(e => e.Meta.AddMetaData(happend.Version + 1, streamName, aggregateName, command))
						.Tap(e => e.Meta.AddTypeInfo(e))
					);

			if (events.Any())
				await store.AppendToStreamAsync(streamName, events.Last().GetVersion(), events);

			await pub(events); //need to always execute due to locks
		}

		public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
			where TState : class, new()
			=> await aggregateLocks.UseLockAsync(command.AggregateId, command.CorrelationId, pub, async (publisher) =>
				 await ExecuteAsync(store, command, action, publisher)
			);
	}
}
