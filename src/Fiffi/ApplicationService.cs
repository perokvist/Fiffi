using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class ApplicationService
    {
        public static Task ExecuteAsync(IEventStore store, ICommand command, string streamName, Func<IEvent[]> action, Func<IEvent[], Task> pub)
            => ExecuteAsync<TestState>(store, command, ("none", streamName), state => Task.FromResult(action()), pub);

        public static Task ExecuteAsync<TState, TEventInterface>(IEventStore store, ICommand command, string streamName, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            where TEventInterface : IEvent
            => ExecuteAsync<TState, TEventInterface>(store, command, (typeof(TState).Name.AsStreamName(command.AggregateId).AggregateName, streamName), state => Task.FromResult(action(state)), pub);

        public static Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            => ExecuteAsync<TState>(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), state => Task.FromResult(action(state)), pub);

        public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command,
            (string aggregateName, string streamName) naming, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
            where TState : class, new()
        {
            if (command.CorrelationId == default)
                throw new ArgumentException("CorrelationId required");

            var happend = await store.LoadEventStreamAsync(naming.streamName, 0);
            var state = happend.Events.Rehydrate<TState>();
            var events = await action(state);

            events.AddMetaData(command, naming.aggregateName, naming.streamName, happend.Version);

            if (events.Any())
                await store.AppendToStreamAsync(naming.streamName, happend.Version, events);

            await pub(events); //need to always execute due to locks
        }

        public static async Task ExecuteAsync<TState, TEventInterface>(IEventStore store, ICommand command,
            (string aggregateName, string streamName) naming, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            where TEventInterface : IEvent
        {
            if (command.CorrelationId == default)
                throw new ArgumentException("CorrelationId required");

            var happend = await store.LoadEventStreamAsync(naming.streamName, 0);
            var state = happend.Events
                .OfType<TEventInterface>()
                .Where(x => x.SourceId == command.AggregateId.Id)
                .Cast<IEvent>()
                .Rehydrate<TState>();
                
            var events = await action(state);

            events.AddMetaData(command, naming.aggregateName, naming.streamName, happend.Version);

            if (events.Any())
                await store.AppendToStreamAsync(naming.streamName, happend.Version, events);

            await pub(events); //need to always execute due to locks
        }

        public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
            where TState : class, new()
            => await aggregateLocks.UseLockAsync(command.AggregateId, command.CorrelationId, pub, async (publisher) =>
                 await ExecuteAsync(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), action, publisher)
            );

        public static Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
            where TState : class, new()
            => ExecuteAsync<TState>(store, command, state => Task.FromResult(action(state)), pub, aggregateLocks);


        public static Task ExecuteAsync<TState>(IStateStore stateManager, ICommand command, Func<TState, IEnumerable<IEvent>> f, Func<IEvent[], Task> pub)
            where TState : class, new()
            => ExecuteAsync(() => stateManager.GetAsync<TState>(command.AggregateId), (state, version, evts) => stateManager.SaveAsync(command.AggregateId, state, version, evts), command, f, pub);

        public static async Task ExecuteAsync<TState>(
            Func<Task<(TState, long)>> getState,
            Func<TState, long, IEvent[], Task> saveState,
            ICommand command,
            Func<TState, IEnumerable<IEvent>> f,
            Func<IEvent[], Task> pub)
            where TState : new()
        {
            var (aggregateName, streamName) = typeof(TState).Name.AsStreamName(command.AggregateId);
            var (state, version) = await getState();
            if (state == null) state = new TState();
            var events = f(state).ToArray();
            var newState = events.Apply(state);

            if (events.Any())
                events.AddMetaData(command, aggregateName, streamName, version);

            await saveState(newState, version, events); //2PC trouble
            await pub(events);
        }

        static void AddMetaData(this IEvent[] events, ICommand command, string aggregateName, string streamName, long version)
        {
            if (!events.All(x => x.SourceId == command.AggregateId.Id)) 
                throw new InvalidOperationException($"Event SourceId not set or not matching the triggering command - {command.GetType()}");

            events
                .Where(x => x.Meta == null)
                .ForEach(x => x.Meta = new Dictionary<string, string>());

            events
                .ForEach(x => x
                        .Tap(e => e.Meta.AddMetaData(version, streamName, aggregateName, command))
                        .Tap(e => e.Meta.AddTypeInfo(e))
                    );
        }
    }
}
