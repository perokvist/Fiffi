using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class Extensions
    {
        public static T Tap<T>(this T self, Action<T> f)
        {
            f(self);
            return self;
        }

        public static T2 Pipe<T, T2>(this T self, Func<T, T2> f) => f(self);

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var item in self)
            {
                action(item);
            }
            return self;
        }

        public static void ForEach<T>(this IEnumerable<T> self, Action<T, int> f)
        {
            var i = 0;
            foreach (var item in self)
            {
                f(item, i);
                i++;
            }
        }

        public static TState Rehydrate<TState>(this IEnumerable<IEvent> events) where TState : new()
            => events.Aggregate(new TState(), (s, @event) => s.Tap(x => ((dynamic)x).When((dynamic)@event)));

        public static TState Apply<TState>(this IEnumerable<IEvent> events, TState currentState) where TState : new()
            => events.Aggregate(currentState, (s, @event) => s.Tap(x => ((dynamic)x).When((dynamic)@event)));

        public static void RegisterReceptor<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, ICommand> receptor)
            where T : IEvent
            => processor.Register<T>(e => d.Dispatch(receptor(e)));

        public static void RegisterReceptor<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, Task<ICommand>> receptor)
            where T : IEvent
            => processor.Register<T>(async e => await d.Dispatch(await receptor(e)));

        public static Action<Func<T, bool>> RegisterReceptorWith<T>(this EventProcessor processor, Dispatcher<ICommand, Task> d, Func<T, ICommand> receptor)
            where T : IEvent
              => f => processor.Register<T>(async e => 
              {
                  if (f(e)) await d.Dispatch(receptor(e));
              });

        public static void Guard<T>(this Action<Func<T, bool>> f, Func<T, bool> guard)
            => f(guard);

        public static void DoIf<T>(this T self, Func<T, bool> p, Action<T> f)
        {
            if (p(self)) f(self);
        }

        public static string AsAggregateName(this string typeName) => typeName.Replace("State", "Aggregate").ToLower();

        public static (string AggregateName, string StreamName) AsStreamName(this string typeName, IAggregateId aggregateId) => (typeName.AsAggregateName(), $"{typeName.AsAggregateName()}-{aggregateId.Id}");

        public static Action<Func<TEvent, Task>> Always<TEvent>(this EventProcessor processor, Func<TEvent, Task> @do)
            where TEvent : IEvent
            => f => processor.Register(@do.Then(f));

        public static Action<Func<TEvent, Task>> When<TEvent>(this Action<Func<TEvent, Task>> f, Func<TEvent, bool> p)
            => next => f(next.When(p));

        public static void Then<TEvent>(this Action<Func<TEvent, Task>> f, Func<TEvent, Task> f2)
            => f(f2.Then(e => Task.CompletedTask));

        public static Func<T, Task> Then<T>(this Func<T, Task> f1, Func<T, Task> f2)
            => e => f1(e).ContinueWith(t => f2(e));

        public static Func<T, Task> When<T>(this Func<T, Task> f, Func<T, bool> p)
            => async e =>
            {
                if (p(e)) await f(e);
            };
    }
}
