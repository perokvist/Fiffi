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
			=> ExecuteAsync<TState>(store, command, state => Task.FromResult(action(state)), pub);

		public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
			where TState : class, new()
		{
			if (command.CorrelationId == default(Guid))
				throw new ArgumentException("CorrelationId required");

			var (aggregateName, streamName) = typeof(TState).Name.AsStreamName(command.AggregateId);
			var happend = await store.LoadEventStreamAsync(streamName, 0);
			var state = happend.Events.Rehydrate<TState>();
			var events = await action(state);

			events.AddMetaData(command, aggregateName, streamName, happend.Version);

			if (events.Any())
				await store.AppendToStreamAsync(streamName, events.Last().GetVersion(), events);

			await pub(events); //need to always execute due to locks
		}

		public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
			where TState : class, new()
			=> await aggregateLocks.UseLockAsync(command.AggregateId, command.CorrelationId, pub, async (publisher) =>
				 await ExecuteAsync(store, command, action, publisher)
			);

		public static Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
			where TState : class, new()
			=> ExecuteAsync<TState>(store, command, state => Task.FromResult(action(state)), pub, aggregateLocks);


		public static Task ExecuteAsync<TState>(IStateManager stateManager, ICommand command, Func<TState, IEnumerable<IEvent>> f, Func<IEvent[], Task> pub)
			where TState : new()
			=> ExecuteAsync(() => stateManager.GetAsync<TState>(command.AggregateId), (state, evts) => stateManager.SaveAsync(command.AggregateId, state, evts), command, f, stateManager.OnPublish(pub));

		public static async Task ExecuteAsync<TState>(
			Func<Task<TState>> getState,
			Func<TState, IEvent[], Task> saveState,
			ICommand command,
			Func<TState, IEnumerable<IEvent>> f,
			Func<IEvent[], Task> pub)
			where TState : new()
		{
			var (aggregateName, streamName) = typeof(TState).Name.AsStreamName(command.AggregateId);
			var state = await getState();
			var events = f(state).ToArray();
			var newState = events.Apply(state);

			if (events.Any())
				events.AddMetaData(command, aggregateName, streamName, events.Last().GetVersion()); //TODO revise version here

			await saveState(state, events); //2PC trouble
			await pub(events);
		}

		static void AddMetaData(this IEvent[] events, ICommand command, string aggregateName, string streamName, long version)
		{
			if (!events.All(x => x.SourceId == command.AggregateId.Id)) throw new InvalidOperationException("Event SourceId not set or not matching the triggering command");

			events
				.Where(x => x.Meta == null)
				.ForEach(x => x.Meta = new Dictionary<string, string>());

			events
				.ForEach(x => x
						.Tap(e => e.Meta.AddMetaData(version + 1, streamName, aggregateName, command))
						.Tap(e => e.Meta.AddTypeInfo(e))
					);
		}
	}
}
