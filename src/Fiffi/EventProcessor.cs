using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
			=> events.ExecuteHandlersAsync(_handlers, BuildExecutionContext,_locks);

		static Func<(Type Type, EventHandle EventHandler), (Task EventHandler, IAggregateId AggregateId, Guid CorrelationId)> BuildExecutionContext(IEvent e)
		=> f => (f.EventHandler(e), new AggregateId(e.AggregateId.ToString()), e.GetCorrelation());

	}

	public class EventProcessor<TAdditional>
	{
		readonly AggregateLocks _locks;
		readonly List<(Type Type, Func<IEvent, TAdditional, Task>)> _handlers = new List<(Type, Func<IEvent, TAdditional, Task>)>();

		public EventProcessor()
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
			=> f => (f.EventHandler(e, additional), new AggregateId(e.AggregateId.ToString()), e.GetCorrelation());
	}

	public static class EventProcessorExtensions
	{
		public static async Task ExecuteHandlersAsync<T>(this IEvent[] events,
			List<(Type, T)> handlers,
			Func<IEvent, Func<(Type Type, T EventHandler), (Task EventHandler, IAggregateId AggregateId, Guid CorrelationId)>> f,
			AggregateLocks locks)
		{
			if (!events.All(e => e.HasCorrelation()))
				throw new ArgumentException("CorrelationId required");

			var executionContext = events.SelectMany(e => handlers
				.Where(e.DelegatefForTypeOrInterface<T>())
				.Select(f(e)))
				.ToArray();

			await Task.WhenAll(executionContext.Select(x => x.EventHandler));

			locks.ReleaseIfPresent(executionContext.Select(x => (x.AggregateId, x.CorrelationId)).ToArray());
		}
		public static Func<(Type Type, THandle EventHandler), bool> DelegatefForTypeOrInterface<THandle>(this IEvent e)
		=> kv => kv.Type == e.GetType() || e.GetType().GetTypeInfo().GetInterfaces().Any(t => t == kv.Type);
	}

}
