namespace Fiffi;
public static partial class ApplicationService
{
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

    public static Task ExecuteAsync(this IEventStore store, ICommand command,
    (string aggregateName, string streamName) naming, Func<IEnumerable<IEvent>, Task<IEvent[]>> action, Func<IEvent[], Task> pub)
    => ExecuteAsync(store, command, naming,
    action, ThrowOnCausation(command), pub);
}
