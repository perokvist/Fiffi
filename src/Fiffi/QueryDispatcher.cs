namespace Fiffi;

using Handlers = Dictionary<Type, Func<object, Task<object>>>;
using StreamHandlers = Dictionary<Type, Func<object, IAsyncEnumerable<object>>>;


public class QueryDispatcher
{
    private readonly Handlers _handlers = new();
    private readonly StreamHandlers _streamHandlers = new();

    public QueryDispatcher RegisterList<TQuery, TResult>(Func<TQuery, Task<IEnumerable<TResult>>> f)
        => _handlers
            .Tap(x => x.Add(typeof(TQuery), async o => await f((TQuery)o)))
            .Pipe(x => this);

    public QueryDispatcher Register<TQuery, TResult>(Func<TQuery, Task<TResult>> f)
        => _handlers
            .Tap(x => x.Add(typeof(TQuery), async o => await f((TQuery)o)))
            .Pipe(x => this);

    public QueryDispatcher Register<TQuery, TResult>(Func<TQuery, IAsyncEnumerable<TResult>> f)
      => _streamHandlers
          .Tap(x => x.Add(typeof(TQuery), o => (IAsyncEnumerable<object>)f((TQuery)o)))
          .Pipe(x => this);

    public Task<object> HandleAsync<TQuery>(TQuery query)
        => _handlers[query.GetType()](query);

    public IAsyncEnumerable<object> HandleStreamAsync<TQuery>(TQuery query)
       => _streamHandlers[query.GetType()](query);
}

