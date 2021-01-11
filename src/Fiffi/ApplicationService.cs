﻿using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class ApplicationService
    {
        public static Task ExecuteAsync(ICommand command, Func<EventRecord[]> action, Func<IEvent[], Task> pub)
        {
            var events = action().ToEnvelopes(command.AggregateId.Id);

            events.AddMetaData(command, string.Empty, string.Empty, 0);

            return pub(events);
        }

        public static Task ExecuteAsync(this IEventStore store, ICommand command, string streamName, Func<EventRecord[]> action, Func<IEvent[], Task> pub)
            => ExecuteAsync<TestState>(store, command, ("none", streamName),
                state => Task.FromResult(action().ToEnvelopes(command.AggregateId.Id)),
                pub);

        public static Task ExecuteAsync(this IEventStore store, ICommand command, string streamName, Func<IEvent[]> action, Func<IEvent[], Task> pub)
            => ExecuteAsync<TestState>(store, command, (null, streamName), state => Task.FromResult(action()), pub);

        public static Task ExecuteAsync<TState, TEvent>(this IEventStore store, ICommand command, string streamName, Func<TState, EventRecord[]> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            where TEvent : EventRecord
            => ExecuteAsync<TState, TEvent>(store,
                command,
                (typeof(TState).Name.AsStreamName(command.AggregateId).AggregateName, streamName),
                state => Task.FromResult(action(state).ToEnvelopes(command.AggregateId.Id)),
                pub);


        public static Task ExecuteAsync<TState, TEvent>(this IEventStore store, ICommand command, string streamName, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            where TEvent : EventRecord
            => ExecuteAsync<TState, TEvent>(store, command, (typeof(TState).Name.AsStreamName(command.AggregateId).AggregateName, streamName), state => Task.FromResult(action(state)), pub);

        public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            => ExecuteAsync<TState>(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), state => Task.FromResult(action(state)), pub);

        public static Task ExecuteAsync(this IEventStore store, ICommand command,
        (string aggregateName, string streamName) naming, Func<IEnumerable<IEvent>, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
        => ExecuteAsync(store, command, naming,
        action, ThrowOnCausation(command), pub);

        public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command,
            (string aggregateName, string streamName) naming, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            => ExecuteAsync(store, command, naming,
                events => action(events.Rehydrate<TState>()),
                ThrowOnCausation(command), pub);


        public static Task ExecuteAsync<TState, TEvent>(this IEventStore store, ICommand command,
            (string aggregateName, string streamName) naming, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
            where TState : class, new()
            where TEvent : EventRecord
            => ExecuteAsync(store, command, naming,
                events => action(events
                .Where(e => e.Event.GetType().BaseType == typeof(TEvent)) //TODO fix!
                .Where(x => x.SourceId == command.AggregateId.Id)
                .Cast<IEvent>()
                .Rehydrate<TState>()),
                None(command), pub);

        public static async Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, Task<IEvent[]>> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
            where TState : class, new()
            => await aggregateLocks.UseLockAsync(command.AggregateId, command.CorrelationId, pub, async (publisher) =>
            await ExecuteAsync(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), action, publisher)
            );

        public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
            where TState : class, new()
            => ExecuteAsync<TState>(store, command, state => Task.FromResult(action(state)), pub, aggregateLocks);

        public static async Task ExecuteAsync(this IEventStore store, ICommand command,
          (string aggregateName, string streamName) naming,
          Func<IEnumerable<IEvent>, Task<IEvent[]>> action,
          Action<IEnumerable<IEvent>> guard,
          Func<IEvent[], Task> pub)
         => await store.ExecuteAsync(naming.streamName, async x =>
         {
             if (command.CorrelationId == default)
                 throw new ArgumentException("CorrelationId required");

             guard(x.events);
             var newEvents = await action(x.events);
             newEvents.AddMetaData(command, naming.aggregateName, naming.streamName, x.version);
             return newEvents;
         }, pub);

        public static async Task ExecuteAsync(this IEventStore store,
            string streamName,
            Func<(IEnumerable<IEvent> events, long version), Task<IEvent[]>> action,
            Func<IEvent[], Task> pub)
        {
            var happend = await store.LoadEventStreamAsync(streamName, 0);

            var events = await action(happend);

            if (events.Any())
                await store.AppendToStreamAsync(streamName, happend.Version, events);

            await pub(events); //need to always execute due to locks        
        }


        public static Task ExecuteAsync<TState>(IStateStore stateManager, ICommand command, Func<TState, IEnumerable<IEvent>> f, Func<IEvent[], Task> pub)
            where TState : class, new()
            => ExecuteAsync(() => stateManager.GetAsync<TState>(command.AggregateId), (state, version, evts) => stateManager.SaveAsync(command.AggregateId, state, version, evts), command, typeof(TState).Name.AsStreamName(command.AggregateId), f, pub);

        public static async Task ExecuteAsync<TState>(
            Func<Task<(TState, long)>> getState,
            Func<TState, long, IEvent[], Task> saveState,
            ICommand command,
            (string aggregateName, string streamName) naming,
            Func<TState, IEnumerable<IEvent>> f,
            Func<IEvent[], Task> pub)
            where TState : new()
        {
            var (aggregateName, streamName) = naming;
            var (state, version) = await getState();
            if (state == null) state = new TState();
            var events = f(state).ToArray();
            var newState = events.Apply(state);

            if (events.Any())
                events.AddMetaData(command, aggregateName, streamName, version);

            await saveState(newState, version, events); //2PC trouble
            await pub(events);
        }

        public static Task ExecuteAsync<TState>(this IEventStore store,
            ICommand command,
            Func<TState, IEvent[]> action,
            Func<IEvent[], Task> pub,
            ISnapshotStore snapshotStore,
            Func<TState, long> getVersion,
            Func<long, TState, TState> setVersion)
            where TState : class, new()
            => ExecuteAsync<TState>(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), action, pub, snapshotStore, getVersion, setVersion);

        public static Task ExecuteAsync<TState>(this IEventStore store,
            ICommand command,
            (string aggregateName, string streamName) naming,
            Func<TState, IEvent[]> action,
            Func<IEvent[], Task> pub,
            ISnapshotStore snapshotStore,
            Func<TState, long> getVersion,
            Func<long, TState, TState> setVersion)
            where TState : class, new()
            => ExecuteAsync(
                async () =>
                {
                    var state = await snapshotStore.Get<TState>(naming.streamName);
                    var (events, version) = await store.LoadEventStreamAsync(naming.streamName, getVersion(state));
                    return (events.Apply(state), version);
                },
                async (newState, version, events) =>
                {
                    if (events.Any())
                    {
                        var newVersion = await store.AppendToStreamAsync(naming.streamName, version, events);
                        await snapshotStore.Apply<TState>(naming.streamName, s =>
                        {
                            return setVersion(newVersion, newState);
                        });
                    }
                },
                command, naming, action, pub
            );

        public static Action<IEnumerable<IEvent>> None(ICommand command)
            => events => { };

        public static Action<IEnumerable<IEvent>> ThrowOnCausation(ICommand command)
         => events =>
         {
             if (events.Any(e => e.GetCausationId() == command.CausationId))
                 throw new Exception($"Duplicate Execution of command based on causation - ({command.CausationId}) - {command.GetType()}. Events with the same causation already exsist.");
         };
    }
}
