using Fiffi.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fiffi.Testing
{
    public static class Extensions
    {
        public static bool Happened(this IEnumerable<IEvent> events) => events.Count() >= 1;

        public static async Task<(object Value, long Version)> GetAsync(this IStateStore stateManager, Type type, IAggregateId aggregateId)
        {
            var mi = typeof(IStateStore).GetMethod("GetAsync").MakeGenericMethod(type);
            return await mi.InvokeAsync<(object, long)>(stateManager, d => (d.Item1, d.Item2), aggregateId);
        }

        public static async Task SaveAsync(this IStateStore stateManager, IAggregateId aggregateId, object state, long version, IEvent[] events, Type type)
        {
            var mi = typeof(IStateStore).GetMethod("SaveAsync").MakeGenericMethod(type);
            await mi.InvokeAsync<bool>(stateManager, d => true, aggregateId, state, version, events);
        }

        static async Task<TResult> InvokeAsync<TResult>(this MethodInfo @this, object obj, Func<dynamic, TResult> f, params object[] parameters)
        {
            dynamic awaitable = @this.Invoke(obj, parameters);
            await awaitable;
            var r = awaitable.GetAwaiter().GetResult();
            return f(r);
        }

        public static IEvent AddTestMetaData<TState>(this IEvent @event, IAggregateId id, int version = 1)
        {
            var (aggregateName, streamName) = typeof(TState).Name.AsStreamName(id);
            @event.Tap(e => e.Meta.AddTypeInfo(e));
            @event.Meta.AddMetaData(version, streamName, aggregateName, new TestCommand(id));
            @event.Meta["test.statetype"] = typeof(TState).AssemblyQualifiedName;
            return @event;
        }
    }
}
