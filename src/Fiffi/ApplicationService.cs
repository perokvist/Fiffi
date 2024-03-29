﻿using Fiffi.Testing;
using System.Data;
using static Fiffi.Extensions;

namespace Fiffi;

public static class ApplicationService
{
    public static Task ExecuteAsync(ICommand command, Func<EventRecord[]> action, Func<IEvent[], Task> pub)
     => pub(action()
            .ToEnvelopes(command.AggregateId.Id)
            .AddMetaData(command, string.Empty, string.Empty, 0)
         );

    public static Task ExecuteAsync(this IEventStore store, ICommand command, string streamName, Func<EventRecord[]> action, Func<IEvent[], Task> pub)
        => ExecuteAsync<TestState>(store, command,
            (null, streamName), new TestState(), (s, e) => s, state => Task.FromResult(action()), pub);

    public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, EventRecord[]> action, Func<IEvent[], Task> pub)
      where TState : class, new()
      => ExecuteAsync<TState>(store, command, 
          typeof(TState).Name.AsStreamName(command.AggregateId), 
          new TState(), (s, e) => new[] { e }.Apply(s),
          state => Task.FromResult(action(state)), pub);

    public static Task ExecuteAsync(this IEventStore store, ICommand command,
    (string aggregateName, string streamName) naming, Func<IEnumerable<IEvent>, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
    => ExecuteAsync(store, command, naming,
    action, ThrowOnCausation(command), pub);

    public static Task ExecuteAsync<TState>(
        this IEventStore store,
        ICommand command,
        string streamName,
        TState defaultState,
        Func<TState, EventRecord, TState> evolve,
        Func<TState, EventRecord[]> action,
        Func<IEvent[], Task> pub)
        where TState : class
        => ExecuteAsync<TState>(store, command, streamName, defaultState, evolve,
            state => Task.FromResult(action(state)), pub);

    public static Task ExecuteAsync<TState>(
    this IEventStore store,
    ICommand command,
    string streamName,
    TState defaultState,
    Func<TState, EventRecord, TState> evolve,
    Func<TState, Task<EventRecord[]>> action, Func<IEvent[], Task> pub)
    where TState : class
        => ExecuteAsync<TState>(store, command, (streamName, streamName), defaultState, evolve, action, pub);

    public static Task ExecuteAsync<TState>(
        this IEventStore store,
        ICommand command,
        (string aggregateName, string streamName) naming,
        TState defaultState,
        Func<TState, EventRecord, TState> evolve,
        Func<TState, Task<EventRecord[]>> action, Func<IEvent[], Task> pub)
        where TState : class
        => ExecuteAsync(store, command, naming,
            async events =>
            {
                var r = await action(events.Select(e => e.Event).Aggregate(defaultState, evolve));
                return r.ToEnvelopes(command.AggregateId.Id);
            },
            ThrowOnCausation(command), pub);


    public static Task ExecuteAsync<TState, TEvent>(this IEventStore store, ICommand command, string streamName, Func<TState, EventRecord[]> action, Func<IEvent[], Task> pub)
        where TState : class, new()
        where TEvent : EventRecord
            => ExecuteAsync<TState, TEvent>(store,
            command,
            (typeof(TState).Name.AsStreamName(command.AggregateId).AggregateName, streamName),
            state => Task.FromResult(action(state)),
            pub);

    public static Task ExecuteAsync<TState, TEvent>(this IEventStore store, ICommand command,
        (string aggregateName, string streamName) naming, Func<TState, Task<EventRecord[]>> action, Func<IEvent[], Task> pub)
        where TState : class, new()
        where TEvent : EventRecord
        => ExecuteAsync(store, command, naming,
            async events =>
            {
                var r = await action(events
                    .Where(e => e.SourceId == command.AggregateId.Id)
                    .Where(e => e.Event.GetType().BaseType == typeof(TEvent)) // TODO fix
                    .Select(x => x.Event)
                    .Rehydrate<TState>());
                return r.ToEnvelopes(command.AggregateId.Id);
            },
            None(command), pub);

    public static async Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, Task<EventRecord[]>> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
        where TState : class, new()
        => await aggregateLocks.UseLockAsync(command.AggregateId, command.CorrelationId, pub, async (publisher) =>
        await ExecuteAsync(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), new TState(), (s, e) => new[] { e }.Apply(s), action, publisher)
        );

    public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, EventRecord[]> action, Func<IEvent[], Task> pub, AggregateLocks aggregateLocks)
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

    public static Task ExecuteAsync<TState>(this IEventStore store,
        ICommand command,
        Func<TState, EventRecord[]> action,
        Func<IEvent[], Task> pub,
        ISnapshotStore snapshotStore,
        Func<TState, long> getVersion,
        Func<long, TState, TState> setVersion)
        where TState : class, new()
        => ExecuteAsync(store, command, typeof(TState).Name.AsStreamName(command.AggregateId),
            new TState(), (state, @event) => new[] { @event }.Apply(state),
            action, pub, snapshotStore, getVersion, setVersion);

    //public static Task ExecuteAsync2<TState>(this IEventStore store,
    //    ICommand command,
    //    string streamName,
    //    TState defaultState,
    //    Func<TState, EventRecord, TState> evolve,
    //    Func<TState, IEnumerable<EventRecord>> action,
    //    Func<IEvent[], Task> pub)
    //    where TState : class
    //     => ExecuteAsync<TState>(
    //         () => {


    //             return Task.FromResult<(TState, long)>((defaultState, 0));
    //         },(s,v,e) => Task.CompletedTask,
    //         command, (streamName, streamName), defaultState ,action, pub);

    public static Task ExecuteAsync<TState>(this IEventStore store,
     ICommand command,
     string streamName,
     TState defaultState,
     Func<TState, EventRecord, TState> evolve,
     Func<TState, EventRecord[]> action,
     Func<IEvent[], Task> pub,
     ISnapshotStore snapshotStore,
     Func<TState, long> getVersion,
     Func<long, TState, TState> setVersion)
     where TState : class
         => ExecuteAsync<TState>(store, command, (streamName, streamName), defaultState, evolve, action, pub, snapshotStore, getVersion, setVersion);


    public static Task ExecuteAsync<TState>(this IEventStore store,
     ICommand command,
     (string aggregateName, string streamName) naming,
     TState defaultState,
     Func<TState, EventRecord, TState> evolve,
     Func<TState, EventRecord[]> action,
     Func<IEvent[], Task> pub,
     ISnapshotStore snapshotStore,
     Func<TState, long> getVersion,
     Func<long, TState, TState> setVersion)
     where TState : class
        => ExecuteAsync(
            async () =>
            {
                var state = await snapshotStore.GetOrCreate($"{naming.streamName}|snapshot", () => defaultState);
                var (events, version) = await store.LoadEventStreamAsync(naming.streamName, new StreamVersion(getVersion(state), Mode.Exclusive));
                return (events.Select(x => x.Event).Aggregate(state, evolve), version);
            },
            async (newState, version, events) =>
            {
                if (events.Any())
                {
                    var newVersion = await store.AppendToStreamAsync(naming.streamName, version, events);
                    await snapshotStore.Apply($"{naming.streamName}|snapshot", defaultState, s =>
                    {
                        return setVersion(newVersion, newState);
                    });
                }
            },
            command, naming, defaultState, evolve, action, pub
        );

    public static Action<IEnumerable<IEvent>> None(ICommand command)
        => events => { };

    public static Action<IEnumerable<IEvent>> ThrowOnCausation(ICommand command)
     => events =>
     {
         if (events.Any(e => e.GetCausationId() == command.CausationId))
             throw new Exception($"Duplicate Execution of command based on causation - ({command.CausationId}) - {command.GetType()}. Events with the same causation already exsist.");
     };

    public static Task ExecuteAsync<TState>(this IStateStore stateManager, ICommand command, Func<TState, IEnumerable<EventRecord>> f, Func<IEvent[], Task> pub)
    where TState : class, new()
    => ExecuteAsync(() => stateManager.GetAsync<TState>(command.AggregateId), (state, version, evts) => stateManager.SaveAsync(command.AggregateId, state, version, evts), command, typeof(TState).Name.AsStreamName(command.AggregateId), f, pub);

    public static Task ExecuteAsync<TState>(
    Func<Task<(TState, long)>> getState,
    Func<TState, long, IEvent[], Task> saveState,
    ICommand command,
    (string aggregateName, string streamName) naming,
    TState defaultState,
    Func<TState, EventRecord, TState> evolve,
    Func<TState, IEnumerable<EventRecord>> f,
    Func<IEvent[], Task> pub)
    where TState : class
    => ExecuteAsync(getState, saveState, command, defaultState, evolve, f,
    (a, version, events) =>
    {
        var (aggregateName, streamName) = naming;
        var envelopes = events.ToEnvelopes(command.AggregateId.Id);
        if (envelopes.Any())
            envelopes.AddMetaData(command, aggregateName, streamName, version);
        return envelopes;
    }, pub);

    public static Task ExecuteAsync<TState>(
        Func<Task<(TState, long)>> getState,
        Func<TState, long, IEvent[], Task> saveState,
        ICommand command,
        (string aggregateName, string streamName) naming,
        Func<TState, IEnumerable<EventRecord>> f,
        Func<IEvent[], Task> pub)
        where TState : class, new()
        => ExecuteAsync(getState, saveState, command, new TState(), (s, e) => new[] { e }.Apply(s), f,
        (a, version, events) =>
        {
            var (aggregateName, streamName) = naming;
            var envelopes = events.ToEnvelopes(command.AggregateId.Id);
            if (envelopes.Any())
                envelopes.AddMetaData(command, aggregateName, streamName, version);
            return envelopes;
        }, pub);

    public static async Task ExecuteAsync<T>(this IEventStore<T> store,
    string streamName,
    Func<(IEnumerable<T> events, long version), Task<T[]>> action,
    Func<T[], Task> pub)
    {
        var happend = await store.LoadEventStreamAsync(streamName, 0);

        var events = await action(happend);

        if (events.Any())
            await store.AppendToStreamAsync(streamName, happend.Version, events);

        await pub(events); //need to always execute due to locks        
    }

    public static async Task ExecuteAsync<TState, TEnvelope, TEvent>(
        Func<Task<(TState, long)>> getState,
        Func<TState, long, TEnvelope[], Task> saveState,
        ICommand command,
        TState defaultState,
        Func<TState, TEvent, TState> evolve,
        Func<TState, IEnumerable<TEvent>> f,
        Func<IAggregateId, long, TEvent[], TEnvelope[]> envFactory,
        Func<TEnvelope[], Task> pub)
        where TState : class
    {
        var (state, version) = await getState();
        state ??= defaultState;
        var events = f(state).ToArray();
        var newState = events.Aggregate(state, evolve);   //events.Apply(state);

        var envelopes = envFactory(command.AggregateId, version, events);

        await saveState(newState, version, envelopes); //2PC trouble
        await pub(envelopes);
    }
}
