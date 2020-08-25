using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;


namespace Fiffi
{
    public static class EventProcessorExtensions
    {
        public static async Task ExecuteHandlersAsync<T>(this IEvent[] events,
            List<(Type, T)> handlers,
            Func<IEvent, Func<(Type Type, T EventHandler), (Task EventHandler, IAggregateId AggregateId, Guid CorrelationId)>> f,
            AggregateLocks locks,
            EventProcessor.DispatchMode mode = EventProcessor.DispatchMode.Parallel)
        {
            if (!events.All(e => e.HasCorrelation()))
                throw new ArgumentException("CorrelationId required");

           var arrayContexts = events.SelectMany(e => handlers
                .Where(h => h.Item1.IsArray)
                .Where(h => typeof(IEvent[]).IsAssignableFrom(h.Item1))
                .Select(f(e)))
                .ToArray();

            var executionContext = events.SelectMany(e => handlers
                .Where(e.DelegatefForTypeOrInterface<T>())
                .Select(f(e)))
                .ToArray();

            var exec = mode switch {
                EventProcessor.DispatchMode.Parallel => Task.WhenAll(arrayContexts.Concat(executionContext).Select(x => x.EventHandler)),
                EventProcessor.DispatchMode.Blocking => arrayContexts.Concat(executionContext).Select(x => x.EventHandler).ForEachAsync(async x => await x),
                EventProcessor.DispatchMode.BatchParallel => Task.WhenAll(Task.WhenAll(arrayContexts.Select(x => x.EventHandler)), executionContext.Select(x => x.EventHandler).ForEachAsync(async x => await x)),
                _ => Task.CompletedTask
            };
            await exec;

            locks.ReleaseIfPresent(executionContext.Select(x => (x.AggregateId, x.CorrelationId)).ToArray());
        }
        public static Func<(Type Type, THandle EventHandler), bool> DelegatefForTypeOrInterface<THandle>(this IEvent e)
        => kv => e.GetType().IsOrImplements(kv.Type);

        public static bool IsOrImplements(this Type e, Type registered)
            => registered.Pipe(t => t == e || e.GetTypeInfo().GetInterfaces().Any(x => x == t));

        public static Action<Func<TEvent, TContext, Task>> Register<TEvent, TContext>(this EventProcessor<TContext> processor)
         where TEvent : IEvent
         => f => processor.Register(f);

        public static Action<Func<TEvent, Task>> Always<TEvent>(this EventProcessor processor, Func<TEvent, Task> @do)
            where TEvent : IEvent
            => f => processor.Register(@do.Then(f));


        public static Action<Func<TEvent, Task>> When<TEvent>(this Action<Func<TEvent, Task>> f, Func<TEvent, bool> p)
            => next => f(next.When(p));

        public static Action<Func<TEvent, TContext, Task>> When<TEvent, TContext>(this Action<Func<TEvent, TContext, Task>> f, Func<TEvent, TContext, bool> p)
            => next => f(next.When(p));

        public static void Then<TEvent>(this Action<Func<TEvent, Task>> f, Func<TEvent, Task> f2)
            => f(f2.Then(e => Task.CompletedTask));

        public static void Then<TEvent, TContext>(this Action<Func<TEvent, TContext, Task>> f, Func<TEvent, TContext, Task> f2)
           => f(f2.Then((e, c) => Task.CompletedTask));
        public static bool Is<T>(this IEvent e)
            where T : IEvent
            => e.Is(typeof(T));

        public static bool Is(this IEvent e, Type type)
            => e.GetType().IsOrImplements(type);

        public static void RegisterReceptor<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, ICommand> receptor)
    where T : IEvent
    => processor.Register<T>(e => d.Dispatch(receptor(e)));

        public static void RegisterReceptor<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, Task<ICommand>> receptor)
            where T : IEvent
            => processor.Register<T>(async e => await d.Dispatch(await receptor(e)));

        public static void Register<TCommand>(this Dispatcher<ICommand, Task> d, params Func<TCommand, Task>[] f)
         where TCommand : ICommand
             => d.Register(f.Aggregate((l, r) => async c =>
             {
                 await l(c);
                 await r(c);
             }));

        public static Action<Func<T, bool>> RegisterReceptorWith<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, ICommand> receptor)
            where T : IEvent
              => f => processor.Register<T>(async e =>
              {
                  if (f(e)) await d.Dispatch(receptor(e));
              });

        public static void RegisterReceptor<TEvent>(this EventProcessor<PolicyContext> ep, ICommand policy)
            where TEvent : IEvent
            => ep.Register<TEvent>((e, ctx) => ctx.ExecuteAsync(policy));


    }
}
