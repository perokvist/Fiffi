using System;
using System.Threading.Tasks;

namespace Fiffi.Modularization
{
    public class ModuleConfiguration<T>
            where T : Module
    {
        private readonly Func<Dispatcher<ICommand, Task>, Func<IEvent[], Task>, QueryDispatcher, T> f;

        public ModuleConfiguration(Func<Dispatcher<ICommand, Task>, Func<IEvent[], Task>, QueryDispatcher, T> f)
        {
            Policies = new EventProcessor<PolicyContext>();
            Projections = new EventProcessor();
            CommandDispatcher = new Dispatcher<ICommand, Task>();
            Queries = new QueryDispatcher();
            this.f = f;
        }

        public EventProcessor<PolicyContext> Policies { get; }
        public EventProcessor Projections { get; }
        public Dispatcher<ICommand, Task> CommandDispatcher { get; }
        public QueryDispatcher Queries { get; }
        public ModuleConfiguration<T> Command<TCommand>(params Func<TCommand, Task>[] f)
            where TCommand : ICommand
        {
            CommandDispatcher.Register(f);
            return this;
        }

        public ModuleConfiguration<T> Projection<TEvent>(Func<TEvent, Task> f)
            where TEvent : IEvent
        {
            Projections.Register(f);
            return this;
        }

        public ModuleConfiguration<T> Projection<TEvent>(Func<TEvent[], Task> f)
        where TEvent : IEvent
        {
            Projections.RegisterAll(f);
            return this;
        }

        public ModuleConfiguration<T> Policy<TEvent>(Func<TEvent, PolicyContext, Task> f)
            where TEvent : IEvent
        {
            Policies.Register(f);
            return this;
        }

        public ModuleConfiguration<T> Query<TQuery, TResult>(Func<TQuery, Task<TResult>> f)
        {
            Queries.Register(f);
            return this;
        }

        public virtual T Create(IEventStore store, EventProcessor.DispatchMode projectionMode = EventProcessor.DispatchMode.Parallel)
        {
            var ep = new EventProcessor<EventContext>();

            ep
            .Register<IEvent, EventContext>()
            .When((e, ctx) => ctx == EventContext.Inbox)
            .Then(EventProcessor.InOrder(
                e => Projections.PublishAsync(projectionMode, e),
                e => Policies.PublishAsync(new PolicyContext(CommandDispatcher, store), e)));

            return f(CommandDispatcher,
            events => ep.PublishAsync(EventContext.Inbox, events),
            Queries);
        }
    }
}
