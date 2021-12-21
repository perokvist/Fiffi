using static Fiffi.Extensions;

namespace Fiffi;
public static partial class ApplicationService
{
    public static Task ExecuteAsync<TState>(this IStateStore stateManager, ICommand command, Func<TState, IEnumerable<EventRecord>> f, Func<IEvent[], Task> pub)
     where TState : class, new()
        => ExecuteAsync(() => stateManager.GetAsync<TState>(command.AggregateId), (state, version, evts) => stateManager.SaveAsync(command.AggregateId, state, version, evts), command, typeof(TState).Name.AsStreamName(command.AggregateId), f, pub);

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
      => ExecuteAsync(
          async () =>
          {
              var state = await snapshotStore.Get<TState>($"{naming.streamName}|snapshot");
              var (events, version) = await store.LoadEventStreamAsync(naming.streamName, new StreamVersion(getVersion(state), Mode.Exclusive));
              return (events.Select(e => e.Event).Apply(state), version);
          },
          async (newState, version, events) =>
          {
              if (events.Any())
              {
                  var newVersion = await store.AppendToStreamAsync(naming.streamName, version, events);
                  await snapshotStore.Apply<TState>($"{naming.streamName}|snapshot", s =>
                  {
                      return setVersion(newVersion, newState);
                  });
              }
          },
          command, naming, action, pub
      );

    public static Task ExecuteAsync<TState>(
      Func<Task<(TState, long)>> getState,
      Func<TState, long, IEvent[], Task> saveState,
      ICommand command,
      (string aggregateName, string streamName) naming,
      Func<TState, IEnumerable<EventRecord>> f,
      Func<IEvent[], Task> pub)
      where TState : class, new()
      => ExecuteAsync(getState, saveState, command, f,
      (a, version, events) =>
      {
          var (aggregateName, streamName) = naming;
          var envelopes = events.ToEnvelopes(command.AggregateId.Id);
          if (envelopes.Any())
              envelopes.AddMetaData(command, aggregateName, streamName, version);
          return envelopes;
      }, pub);

    public static async Task ExecuteAsync<TState, TEnvelope, TEvent>(
    Func<Task<(TState, long)>> getState,
    Func<TState, long, TEnvelope[], Task> saveState,
    ICommand command,
    Func<TState, IEnumerable<TEvent>> f,
    Func<IAggregateId, long, TEvent[], TEnvelope[]> envFactory,
    Func<TEnvelope[], Task> pub)
    where TState : class, new()
    {
        var (state, version) = await getState();
        if (state == null) state = new TState();
        var events = f(state).ToArray();
        var newState = events.Apply(state);

        var envelopes = envFactory(command.AggregateId, version, events);

        await saveState(newState, version, envelopes); //2PC trouble
        await pub(envelopes);
    }
}
