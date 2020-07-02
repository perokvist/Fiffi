using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fiffi.Testing
{
    public static class Extensions
    {
        public static void Given<TState>(this ITestContext context, IAggregateId id, params IEvent[] events)
            => context.Given(events.Select(e => e.AddTestMetaData<TState>(id)).ToArray());

        public static bool Happened(this IEnumerable<IEvent> events) => events.Count() >= 1;

        public static async Task<(object Value, long Version)> GetAsync(this IStateStore stateManager, Type type, IAggregateId aggregateId)
        {
            var mi = typeof(IStateStore).GetMethod("GetAsync").MakeGenericMethod(type);
            return await mi.InvokeAsync<(object, long)>(stateManager, d => (d.Item1, d.Item2), true, aggregateId);
        }

        public static async Task SaveAsync(this IStateStore stateManager, IAggregateId aggregateId, object state, long version, IEvent[] events, Type type)
        {
            var mi = typeof(IStateStore).GetMethod("SaveAsync").MakeGenericMethod(type);
            await mi.InvokeAsync<bool>(stateManager, d => true, false, aggregateId, state, version, events);
        }

        static async Task<TResult> InvokeAsync<TResult>(this MethodInfo @this, object obj, Func<dynamic, TResult> f, bool withResult, params object[] parameters)
        {
            dynamic awaitable = @this.Invoke(obj, parameters);
            await awaitable;
            if (withResult)
            {
                var r = awaitable.GetAwaiter().GetResult();
                return f(r);
            }

            return f(null);
        }

        public static IEvent AddTestMetaData<TState>(this IEvent @event, IAggregateId id, int version = 0)
        {
            var (aggregateName, streamName) = typeof(TState).Name.AsStreamName(id);
            if (@event.Meta == null) @event.Meta = new Dictionary<string, string>();
            @event.Tap(e => e.Meta.AddTypeInfo(e));
            @event.Meta.AddMetaData(version, streamName, aggregateName, new TestCommand(id));
            @event.Meta["test.statetype"] = typeof(TState).AssemblyQualifiedName;
            return @event;
        }

        public static IEvent AddTestMetaData<TProjection>(this IEvent @event, string streamName, int version = 0)
        {
            @event.Tap(e => e.Meta.AddTypeInfo(e));
            @event.Meta.AddMetaData(version, streamName, "test-projection", new TestCommand(new AggregateId(Guid.NewGuid())));
            @event.Meta["test.statetype"] = typeof(TProjection).AssemblyQualifiedName;
            return @event;
        }

        public static IEvent AddTestMetaData(this IEvent @event, string streamName, int version = 0)
        {
            @event.Tap(e => e.Meta.AddTypeInfo(e));
            @event.Meta.AddMetaData(version, streamName, "test-stream", new TestCommand(new AggregateId(Guid.NewGuid())));
            return @event;
        }

        public static Func<IEvent[], Task> AsPub(this Queue<IEvent> q) => events =>
        {
            events.ForEach(e => q.Enqueue(e));
            return Task.CompletedTask;
        };
    }
}
