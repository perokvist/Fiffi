namespace Fiffi.Modularization;

public abstract class Module : IModule
{
    public record ModuleCore(
        Func<ICommand, Task> Dispatcher,
        Func<IEvent[], Task> Publish,
        QueryDispatcher QueryDispatcher,
        Func<IEvent[], Task> OnStart
        );

    readonly Func<IEvent[], Task> onStart;
    readonly Func<ICommand, Task> dispatcher;
    readonly QueryDispatcher queryDispatcher;
    readonly Func<IEvent[], Task> publish;

    public Module(ModuleCore core) : this(core.Dispatcher, core.Publish, core.QueryDispatcher, core.OnStart)
    { }

    public Module(
        Func<ICommand, Task> dispatcher,
        Func<IEvent[], Task> publish,
        QueryDispatcher queryDispatcher)
        : this(dispatcher, publish, queryDispatcher, events => Task.CompletedTask)
    { }

    public Module(
        Func<ICommand, Task> dispatcher,
        Func<IEvent[], Task> publish,
        QueryDispatcher queryDispatcher,
        Func<IEvent[], Task> onStart)
    {
        this.dispatcher = dispatcher;
        this.publish = publish;
        this.queryDispatcher = queryDispatcher;
        this.onStart = onStart;
    }

    public Task DispatchAsync(ICommand command) => this.dispatcher(command);

    public async Task<T> QueryAsync<T>(IQuery<T> q) where T : class
        => (T)await queryDispatcher.HandleAsync(q);

    public IAsyncEnumerable<T> QueryAsync<T>(IStreamQuery<T> q)
        => (IAsyncEnumerable<T>)queryDispatcher.HandleStreamAsync(q);

    public Task WhenAsync(params IEvent[] events) => publish(events);

    internal Task OnStart(IEvent[] events) => onStart(events);

    Task IModule.OnStart(IEvent[] events) => this.OnStart(events);
}
