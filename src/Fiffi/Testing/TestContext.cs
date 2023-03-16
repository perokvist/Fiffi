using Fiffi.Modularization;

namespace Fiffi.Testing;

public class TestContext : ITestContext
{
    IEvent[] events = { };
    readonly Queue<IEvent> q;
    readonly IModule module;
    readonly Func<IEvent, Task>[] whens;
    readonly Func<IEvent[], Func<IEventStore, Task>, Task> init;

    public TestContext(
        Func<IEvent[], Func<IEventStore, Task>, Task> init,
        Queue<IEvent> q,
        IModule module, 
        params Func<IEvent, Task>[] whens)
    {
        this.init = init;
        this.whens = whens;
        this.q = q;
        this.module = module;
    }

    public void Given(params IEvent[] events)
        => Given(Array.Empty<string>(), Foo(events), events);

    public void Given(string[] streams, StreamName aggregateStream = StreamName.FromMeta, params IEvent[] events)
     => Do(aggregateStream switch
        {
            StreamName.FromMeta => () => events
               .GroupBy(x => x.GetStreamName()) //TODO version and position ?
               .ForEach(x => Given(new[] { x.Key }.Concat(streams).ToArray(), x)),
            StreamName.FromSourceId => () => events.GroupBy(x => x.SourceId)
               .ForEach(x => Given(new[] { x.Key }.Concat(streams).ToArray(), x)),
            _ => () => Given(streams, events)
        });

    static StreamName Foo(params IEvent[] events)
     => events.All(e => e.HasMeta(nameof(EventMetaData.StreamName))) switch
        {
            true => StreamName.FromMeta,
            _ => StreamName.FromSourceId
        };

    static void Do(Action a) => a();

    //    if (aggregateStream == StreamName.FromMeta)
    //        events
    //          .GroupBy(x => x.GetStreamName()) //TODO version and position ?
    //         .ForEach(x => Given(new[] { x.Key }.Concat(streams).ToArray(), x));
    //    else
    //        Given(streams, events);
    //}

    public void Given(string[] streamNames, IEnumerable<IEvent> events)
            => this.init(events.ToArray(), store => Task.WhenAll(streamNames
                 .Select(streamName => store.AppendToStreamAsync(streamName, 0, events.ToArray()))))
                .GetAwaiter().GetResult();

    public Task WhenAsync(IEvent @event)
        => WhenAsync(() => Task.WhenAll(this.whens.Select(w => w(@event))));

    public Task WhenAsync(ICommand command)
     => WhenAsync(() => module.DispatchAsync(command));

    public async Task WhenAsync(Func<Task> f)
    {
        await f();
        while (this.q.Any())
        {
            var e = this.q.Dequeue();
            await Task.WhenAll(this.whens.Select(w => w(e)));
            this.events = this.events.Concat(new IEvent[] { e }).ToArray();
        }
    }

    public void Then(Action<IEvent[]> f) => f(this.events);

    public async Task ThenAsync<T>(IQuery<T> q, Action<T> f)
        where T : class
        => f((await module.QueryAsync(q)));
   
    public enum StreamName
    {
        FromMeta = 10,
        FromStreamName = 50,
        FromSourceId = 100
    }
}
