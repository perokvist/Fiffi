using Fiffi;
using Fiffi.Testing;
using System.Data;

namespace Fiffi;

public delegate Task EventAppService<T>(IEventStore<T> store, string streamName, long fromVersion,
    Func<(IEnumerable<T> events, long version), Task<T[]>> f, Func<T[], Task> pub);

public delegate Task StateAppService<TState>(IEventStore<IEvent> store, string streamName, ICommand command,
    Func<TState, Task<EventRecord[]>> f, Func<IEvent[], Task> pub);

public delegate StateAppService<TState> StateAppServiceBuilder<TState, TEnv>(EventAppService<TEnv> eventDecider);

public delegate TEnvelope[] EnvelopeCreator<T, TEnvelope>(ICommand command, (string aggregateName, string streamName) naming, long basedOnVersion, T[] events);

public delegate TState Evolve<TState>(TState initialState, EventRecord[] events);

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

    public static Task ExecuteAsync<TState>(this IEventStore store,
   ICommand command,
   Func<TState, EventRecord[]> action,
   Func<IEvent[], Task> pub,
   ISnapshotStore snapshotStore,
   Func<TState, long> getVersion,
   Func<long, TState, TState> setVersion)
   where TState : class, new()
   => ExecuteAsync<TState>(store, command, typeof(TState).Name.AsStreamName(command.AggregateId), action, pub, snapshotStore, getVersion, setVersion);

    public static Task ExecuteAsync<TState>(this IEventStore store,
   ICommand command,
   (string aggregateName, string streamName) naming,
   Func<TState, EventRecord[]> action,
   Func<IEvent[], Task> pub,
   ISnapshotStore snapshotStore,
   Func<TState, long> getVersion,
   Func<long, TState, TState> setVersion)
   where TState : class, new()
    {
        var exec = Build(CreateEnvelope(),
                   (state, events) => events.Apply(state),
                   snapshotStore,
                   getVersion, setVersion)(ExecuteAsync);
        return exec(store, naming.streamName, command, state => Task.FromResult(action(state)), pub);
    }

    public static StateAppServiceBuilder<TState, IEvent> Execute<TState>(
        EnvelopeCreator<EventRecord, IEvent> envelopeCreator,
        Func<EventRecord[], TState> evolve)
        => eventDecider => async (store, streamName, command, f, pub) =>
        {
            await eventDecider(store, streamName, 0, async events =>
            {
                var state = evolve(events.events.Select(x => x.Event).ToArray());
                var result = await f(state);
                return envelopeCreator(command, (typeof(TState).Name.AsAggregateName(), streamName), events.version, result);
            }, pub);
        };

    public static StateAppServiceBuilder<TState, IEvent> Build<TState>(
        EnvelopeCreator<EventRecord, IEvent> envelopeCreator,
        Func<TState, EventRecord[], TState> evolve,
        ISnapshotStore snapshotStore,
        Func<TState, long> getVersion,
        Func<long, TState, TState> setVersion)
        where TState : class, new()
        => eventDecider => async (store, streamName, command, f, pub) =>
       {
           var snapshot = await snapshotStore.Get<TState>(streamName);
           var snapVersion = getVersion(snapshot);
           var version = snapVersion;
           TState state = default;
           await eventDecider(store, streamName, snapVersion, async readResult =>
           {
               version = readResult.version;
               state = evolve(snapshot, readResult.events.Select(x => x.Event).ToArray());
               var result = await f(state);
               return envelopeCreator(command, (typeof(TState).Name.AsAggregateName(), streamName), readResult.version, result);
           }, async events =>
           {
               //TODO add versionto pub ?
               var newState = evolve(state, events.Select(x => x.Event).ToArray());
               await snapshotStore.Apply<TState>($"{streamName}|snapshot", s =>
                   setVersion(version + events.Length, newState)
               );
               await pub(events);
           });
       };

    public static EventAppService<TEnv> Intercept<TEnv>(Func<TEnv[], TEnv[]> intercept, EventAppService<TEnv> eventDecider)
       => (store, streamName, fromVersion, f, pub) =>
         eventDecider(store, streamName, fromVersion,
             async readResult =>
             {
                 var filtered = intercept(readResult.events.ToArray());
                 return await f((filtered, readResult.version));
             },
             pub);

    public static EventAppService<TEnv> WithMeta<TEnv>(
        ICommand command,
        (string aggregateName, string streamName) naming,
        EnvelopeCreator<TEnv, TEnv> meta,
        EventAppService<TEnv> eventDecider)
      => (store, streamName, fromVersion, f, pub) =>
        eventDecider(store, streamName, fromVersion,
            async readResult =>
            {
                var newEvents = await f(readResult);
                var r = meta(command, naming, readResult.version, newEvents);
                return r;
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
