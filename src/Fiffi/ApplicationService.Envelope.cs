namespace Fiffi;
public static partial class ApplicationService
{
    public static EnvelopeCreator<EventRecord, IEvent> CreateEnvelope()
     => (command, naming, basedOnVersion, events) =>
     {
         var envelopes = events.ToEnvelopes(command.AggregateId.Id);
         envelopes.AddMetaData(command, naming.aggregateName, naming.streamName, basedOnVersion);
         return envelopes;
     };

    public static EnvelopeCreator<IEvent, IEvent> EnvelopeMeta()
    => (command, naming, basedOnVersion, events) =>
    {
        //var newEvents = events
        //.Select(e => EventEnvelope.Create(command.AggregateId.Id, e.Event)
        //.Tap(x => x.Meta = e.Meta));
        return events.AddMetaData(command, naming.aggregateName, naming.streamName, basedOnVersion);
    };

    public static Task ExecuteAsync(this IEventStore store, ICommand command,
    (string aggregateName, string streamName) naming,
    Func<IEnumerable<IEvent>, Task<IEvent[]>> action,
    Action<IEnumerable<IEvent>> guard,
    Func<IEvent[], Task> pub)
     => WithMeta(command, naming, EnvelopeMeta(), ExecuteAsync)
        .Pipe(meta => Intercept(events =>
        {
            if (command.CorrelationId == default)
                throw new ArgumentException("CorrelationId required");
            guard(events);
            return events;
        }, meta))
        (store, naming.streamName, 0, async readResult =>
        {
            var newEvents = await action(readResult.events);
            return newEvents.AddMetaData(command, naming.aggregateName, naming.streamName, readResult.version);
        }, pub);

    public static Task ExecuteAsync(this IEventStore store, ICommand command,
    (string aggregateName, string streamName) naming, Func<IEnumerable<IEvent>, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
    => ExecuteAsync(store, command, naming,
    action, ThrowOnCausation(command), pub);
}
