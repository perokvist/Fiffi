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

        public static async Task<object> GetAsync(this IStateStore stateManager, Type type, IAggregateId aggregateId)
        {
            var mi = typeof(IStateStore).GetMethod("GetAsync").MakeGenericMethod(type);
            return await mi.InvokeAsync(stateManager, aggregateId);
        }

        public static async Task SaveAsync(this IStateStore stateManager, IAggregateId aggregateId, object state, IEvent[] events, Type type)
        {
            var mi = typeof(IStateStore).GetMethod("SaveAsync").MakeGenericMethod(type);
            await mi.InvokeAsync(stateManager, aggregateId, events);
        }

        public static async Task<object> InvokeAsync(this MethodInfo @this, object obj, params object[] parameters)
        {
            dynamic awaitable = @this.Invoke(obj, parameters);
            await awaitable;
            return awaitable.GetAwaiter().GetResult();
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
