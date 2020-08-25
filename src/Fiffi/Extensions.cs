﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        public static async Task ForEachAsync<T>(this IEnumerable<T> self, Func<T, Task> action)
        {
            foreach (var item in self)
            {
                await action(item);
            }
        }

        public static TState Rehydrate<TState>(this IEnumerable<IEvent> events) where TState : new()
            => events.Aggregate(new TState(), (s, @event) => ((dynamic)s).When((dynamic)@event));

        public static TState Apply<TState>(this IEnumerable<IEvent> events, TState currentState) where TState : new()
            => events.Aggregate(currentState, (s, @event) => ((dynamic)s).When((dynamic)@event));

        public static IEvent[] Filter(this IEnumerable<IEvent> events, params Type[] include)
            => events
            .Where(x => include.Any(t => t.Equals(x.GetType())))
            .ToArray();

        public static void Guard<T>(this Action<Func<T, bool>> f, Func<T, bool> guard)
            => f(guard);

        public static void DoIf<T>(this T self, Func<T, bool> p, Action<T> f)
        {
            if (p(self)) f(self);
        }

        public static async Task DoIfAsync<T>(this T self, Func<T, bool> p, Func<T, Task> f)
        {
            if (p(self))
            {
                await f(self);
            }
        }

        public static object GetDefault(this Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }

        public static string AsAggregateName(this string typeName) => typeName.Replace("State", "Aggregate").ToLower();

        public static (string AggregateName, string StreamName) AsStreamName(this string typeName, AggregateId aggregateId)
            => typeName.AsStreamName((IAggregateId)aggregateId);

        public static (string AggregateName, string StreamName) AsStreamName(this string typeName, IAggregateId aggregateId) => (typeName.AsAggregateName(), $"{typeName.AsAggregateName()}-{aggregateId.Id}");

        public static Func<T, Task> Then<T>(this Func<T, Task> f1, Func<T, Task> f2)
            => async e =>
            {
                await f1(e);
                await f2(e);
            };

        public static Func<T, T2, Task> Then<T, T2>(this Func<T, T2, Task> f1, Func<T, T2, Task> f2)
            => async (e, c) =>
            {
                await f1(e, c);
                await f2(e, c);
            };

        public static Func<T, Task> When<T>(this Func<T, Task> f, Func<T, bool> p)
            => async e =>
            {
                if (p(e)) await f(e);
            };

        public static Func<T, T2, Task> When<T, T2>(this Func<T, T2, Task> f, Func<T, T2, bool> p)
           => async (e, c) =>
           {
               if (p(e, c)) await f(e, c);
           };

        public static Func<T, T> Combine<T>(params Func<Func<T, T>, Func<T, T>>[] funcs)
            => input => funcs.Aggregate((l, r) => f => l(r(f)))(c => c)(input);

        public static Func<T, Task> Combine<T>(params Func<Func<T, Task>, Func<T, Task>>[] funcs)
            => input => funcs.Aggregate((l, r) => f => l(r(f)))(c => Task.CompletedTask)(input);
    }
}
