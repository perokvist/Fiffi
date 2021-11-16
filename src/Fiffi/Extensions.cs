using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi;

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

    public static TState Rehydrate<TState>(this IEnumerable<EventRecord> events) where TState : new()
        => events.Aggregate(new TState(), (s, @event) => ((dynamic)s).When(@event));

    public static TState Apply<TState, TEvent>(this IEnumerable<TEvent> events, TState currentState) where TState : class
     => events.Aggregate(currentState, (s, @event) => ((dynamic)s).When((@event)));

    public static TState Apply<TState>(this IEnumerable<EventRecord> events, TState currentState, Func<TState, EventRecord, TState> apply) where TState : class
     => events.Aggregate(currentState, apply);


    public static IEvent[] Filter(this IEnumerable<IEvent> events, params Type[] include)
        => events
        .Where(x => include.Any(t => t.Equals(x.Event.GetType())))
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
    public static Func<T, T> Combine<T>(params Func<Func<T, T>, Func<T, T>>[] funcs)
        => input => funcs.Aggregate((l, r) => f => l(r(f)))(c => c)(input);

    public static Func<T, Task> Combine<T>(params Func<Func<T, Task>, Func<T, Task>>[] funcs)
        => input => funcs.Aggregate((l, r) => f => l(r(f)))(c => Task.CompletedTask)(input);

    public static Task<(IEnumerable<T> Events, long Version)> LoadEventStreamAsync<T>(this IEventStore<T> store, string streamName, StreamVersion version)
        => store.LoadEventStreamAsync(streamName, version.mode switch
        {
            Mode.Inclusive => version.version,
            Mode.Exclusive => version.version + 1,
            _ => throw new NotImplementedException($"{version.mode} not supported")
        });

    public record StreamVersion(long version, Mode mode = Mode.Inclusive);

    public enum Mode
    {
        Inclusive,
        Exclusive
    }
}
