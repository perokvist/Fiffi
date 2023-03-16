namespace Fiffi.Modularization;

public class Configuration<T>
    where T : Module
{
    readonly Func<Func<ICommand, Task>, Func<IEvent[], Task>, QueryDispatcher, Func<IEvent[], Task>, T> f;

    public Configuration(Func<Module.ModuleCore, T> f)
        : this((d, p, q, s) => f(new(d, p, q, s)))
    { }

    public Configuration(Func<Func<ICommand, Task>, Func<IEvent[], Task>, QueryDispatcher, Func<IEvent[], Task>, T> f)
    {
        this.f = f;
    }

    Func<ICommand, Task> dispatch;
    readonly List<Func<IEvent[], Task>> updates = new();
    readonly List<Func<IEvent[], Func<IEvent, ICommand, Task>, Task>> triggers = new();
    readonly QueryDispatcher queries = new();

    public Configuration<T> Commands(params Func<ICommand, Task>[] f)
     => this.Tap(x => x.dispatch = f.Aggregate((l, r) => async c =>
        {
            try
            {
                await l(c);
                await r(c);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error handling {c.GetType().Name} - {c.AggregateId}", ex);
            }
        }));

    public Configuration<T> Updates(Func<IEvent[], Task> f)
     => this.Tap(x => x.updates.Add(f));

    public Configuration<T> Triggers(Func<IEvent[], Func<IEvent, ICommand, Task>, Task> f)
     => this.Tap(x => x.triggers.Add(f));

    public Configuration<T> Query<TQuery, TResult>(Func<TQuery, Task<TResult>> f)
     => this.Tap(x => x.queries.Register(f));

    public Configuration<T> QueryStream<TQuery, TResult>(Func<TQuery, IAsyncEnumerable<TResult>> f)
     => this.Tap(x => x.queries.Register(f));

    public virtual T Create(IEventStore store) => f(dispatch, async events =>
    {
        if (!events.Any())
            return;
        await Task.WhenAll(updates.Select(x => x(events))); //TODO config for grouping by source ?
        await Task.WhenAll(triggers.Select(t => t(events, (e, cmd) =>
        {
            if (cmd != null) return dispatch(Policy.Issue(e, () => cmd));
            return Task.CompletedTask;
        })));
    }, queries, x => Task.WhenAll(updates.Select(u => u(x))));
}
