using System;
using System.Threading.Tasks;

namespace Fiffi.Modularization
{
    public abstract class Module
    {
        Dispatcher<ICommand, Task> Dispatcher { get; }
        QueryDispatcher QueryDispatcher { get; }
        Func<IEvent[], Task> Publish { get; }

        public Module(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher)
        {
            Dispatcher = dispatcher;
            Publish = publish;
            QueryDispatcher = queryDispatcher;
        }

        public Task DispatchAsync(ICommand command) => this.Dispatcher.Dispatch(command);

        public async Task<T> QueryAsync<T>(IQuery<T> q)
            => (T)await QueryDispatcher.HandleAsync(q);

        public Task WhenAsync(params IEvent[] events) => Publish(events);
    }
}
