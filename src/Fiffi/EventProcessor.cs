using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Fiffi
{
    using EventHandle = Func<IEvent, Task>;

    public class EventProcessor
    {

        readonly AggregateLocks _locks;
        readonly List<(Type Type, EventHandle EventHandler)> _handlers = new List<(Type, EventHandle)>();


        public EventProcessor() : this(new AggregateLocks())
        { }


        public EventProcessor(AggregateLocks locks)
        {
            _locks = locks;
        }


        public void Register<T>(Func<T, Task> f)
            where T : IEvent
            => _handlers.Add((typeof(T), @event => f((T)@event)));


        public Task PublishAsync(params IEvent[] events)
            => events.ExecuteHandlersAsync(_handlers, BuildExecutionContext, _locks);

        static Func<(Type Type, EventHandle EventHandler), (Task EventHandler, IAggregateId AggregateId, Guid CorrelationId)> BuildExecutionContext(IEvent e)
        => f => (f.EventHandler(e), new AggregateId(e.SourceId), e.GetCorrelation());

        public static Func<IEvent, EventContext, Task> InOrder(params Func<IEvent, Task>[] orderActions)
            => async (e, ctx) =>
        {
            foreach (var a in orderActions)
            {
                await a(e);
            }
        };
    }

    public class EventProcessor<TAdditional>
    {
        readonly AggregateLocks _locks;
        readonly List<(Type Type, Func<IEvent, TAdditional, Task>)> _handlers = new List<(Type, Func<IEvent, TAdditional, Task>)>();

        public EventProcessor() : this(new AggregateLocks())
        { }

        public EventProcessor(AggregateLocks locks)
        {
            _locks = locks;
        }

        public void Register<T>(Func<T, TAdditional, Task> f)
            where T : IEvent
            => _handlers.Add((typeof(T), (@event, additional) => f((T)@event, additional)));

        public Task PublishAsync(TAdditional additional, params IEvent[] events)
        => events.ExecuteHandlersAsync(_handlers, e => BuildExecutionContext(e, additional), _locks);

        static Func<(Type Type, Func<IEvent, TAdditional, Task> EventHandler), (Task EventHandler, IAggregateId AggregateId, Guid CorrelationId)> BuildExecutionContext(IEvent e, TAdditional additional)
            => f => (f.EventHandler(e, additional), new AggregateId(e.SourceId), e.GetCorrelation());
    }
}
