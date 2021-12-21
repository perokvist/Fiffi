using static Fiffi.Extensions;

namespace Fiffi;
public static partial class ApplicationService
{
    public static Task ExecuteAsync<TState>(this IStateStore stateManager, ICommand command, Func<TState, IEnumerable<EventRecord>> f, Func<IEvent[], Task> pub)
     where TState : class, new()
        => ExecuteAsync(() => stateManager.GetAsync<TState>(command.AggregateId), (state, version, evts) => stateManager.SaveAsync(command.AggregateId, state, version, evts), command, typeof(TState).Name.AsStreamName(command.AggregateId), f, pub);

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
