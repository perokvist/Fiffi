using Fiffi;
using Fiffi.Testing;
using System.Data;

namespace Fiffi;

public delegate Task EventDecider<T>(IEventStore<T> store, string streamName, long fromVersion,
    Func<(IEnumerable<T> events, long version), Task<T[]>> f, Func<T[], Task> pub);

public delegate Task StateDecider<TState>(IEventStore<IEvent> store, string streamName, IAggregateId aggregateId,
    Func<TState, Task<EventRecord[]>> f, Func<IEvent[], Task> pub);

public delegate StateDecider<TState> StateDeciderBuilder<TState, TEnv>(EventDecider<TEnv> eventDecider);

public delegate TEnvelope[] EnvelopeCreator<T, TEnvelope>(IAggregateId sourceId, long basedOnVersion, T[] events);


public static partial class ApplicationService
{
    public static Task ExecuteAsync(ICommand command, Func<EventRecord[]> action, Func<IEvent[], Task> pub)
     => pub(action()
            .ToEnvelopes(command.AggregateId.Id)
            .AddMetaData(command, string.Empty, string.Empty, 0)
         );

    public static Task ExecuteAsync(this IEventStore store, ICommand command, string streamName, Func<EventRecord[]> action, Func<IEvent[], Task> pub)
        => ExecuteAsync<TestState>(store, command, (null, streamName), state => Task.FromResult(action()), pub);

    public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command, Func<TState, EventRecord[]> action, Func<IEvent[], Task> pub)
      where TState : class, new()
      => ExecuteAsync<TState>(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), state => Task.FromResult(action(state)), pub);

    public static Task ExecuteAsync<TState>(this IEventStore store, ICommand command,
        (string aggregateName, string streamName) naming, Func<TState, Task<EventRecord[]>> action, Func<IEvent[], Task> pub)
        where TState : class, new()
        => ExecuteAsync(store, command, naming,
            async events =>
            {
                var r = await action(events.Select(e => e.Event).Rehydrate<TState>());
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


    public static StateDeciderBuilder<TState, IEvent> Execute<TState>(
        EnvelopeCreator<EventRecord, IEvent> envelopeCreator,
        Func<EventRecord[], TState> evolve)
        => eventDecider => async (store, streamName, aggregateId, f, pub) =>
        {
            await eventDecider(store, streamName, 0, async events =>
            {
                var state = evolve(events.events.Select(x => x.Event).ToArray()); 
                var result = await f(state);
                return envelopeCreator(aggregateId, events.version, result);
            }, pub);
        };

    public static StateDeciderBuilder<TState, IEvent> Execute<TState>(
        EnvelopeCreator<EventRecord, IEvent> envelopeCreator,
        Func<TState, EventRecord[], TState> evolve,
        ISnapshotStore snapshotStore,
        Func<TState, long> getVersion,
        Func<long, TState, TState> setVersion)
        where TState : class, new()
        => eventDecider =>  async (store, streamName, aggregateId, f, pub) =>
        {
            var snapshot = await snapshotStore.Get<TState>(streamName);
            var snapVersion = getVersion(snapshot);
            await eventDecider(store, streamName, snapVersion, async events =>
            {
                var state = evolve(snapshot, events.events.Select(x => x.Event).ToArray());
                var result = await f(state);
                return envelopeCreator(aggregateId, events.version, result);
            }, async events => { 
                await snapshotStore.Apply<TState>(streamName, events);
                await pub(events);
            });
        };

    public static EventDecider<TEnv> Intercept<TEnv>(Func<TEnv[], TEnv[]> intercept, EventDecider<TEnv> eventDecider)
       => (store, streamName, fromVersion, f, pub) =>
         eventDecider(store, streamName, fromVersion,
             async events => {
                 var filtered = intercept(events.events.ToArray());
                 return await f((filtered, events.version));
             }, 
             pub);
 
    public static Task ExecuteAsync<T>(this IEventStore<T> store,
    string streamName,
    Func<(IEnumerable<T> events, long version), Task<T[]>> action,
    Func<T[], Task> pub)
        => ExecuteAsync<T>(store, streamName, 0, action, pub);

    public static async Task ExecuteAsync<T>(this IEventStore<T> store,
    string streamName,
    long fromVersion,
    Func<(IEnumerable<T> events, long version), Task<T[]>> action,
    Func<T[], Task> pub)
    {
        var happend = await store.LoadEventStreamAsync(streamName, fromVersion);

        var events = await action(happend);

        if (events.Any())
            await store.AppendToStreamAsync(streamName, happend.Version, events);

        await pub(events); //need to always execute due to locks        
    }


}
