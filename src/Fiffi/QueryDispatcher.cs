namespace Fiffi;

using Handlers = Dictionary<Type, Func<object, Task<object>>>;

public class QueryDispatcher
{
    private readonly Handlers _handlers = new Handlers();

    public QueryDispatcher RegisterList<TQuery, TResult>(Func<TQuery, Task<IEnumerable<TResult>>> f)
        => _handlers
            .Tap(x => x.Add(typeof(TQuery), async o => await f((TQuery)o)))
            .Pipe(x => this);

    public QueryDispatcher Register<TQuery, TResult>(Func<TQuery, Task<TResult>> f)
        => _handlers
            .Tap(x => x.Add(typeof(TQuery), async o => await f((TQuery)o)))
            .Pipe(x => this);


    public Task<object> HandleAsync<TQuery>(TQuery query)
        => _handlers[query.GetType()](query);
}
