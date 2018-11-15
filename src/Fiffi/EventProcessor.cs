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


		public async Task PublishAsync(params IEvent[] events)
		{
			if (!events.All(e => e.Meta.ContainsKey(nameof(EventMetaData.CorrelationId))))
				throw new ArgumentException("CorrelationId required");

			var executionContext = events.SelectMany(e => _handlers
				.Where(DelegatefForTypeOrInterface(e))
				.Select(BuildExecutionContext(e)))
				.ToArray();

			await Task.WhenAll(executionContext.Select(x => x.EventHandler));

			_locks.ReleaseIfPresent(executionContext.Select(x => (x.AggregateId, x.CorrelationId)).ToArray());
		}

		static Func<(Type Type, EventHandle EventHandler), (Task EventHandler, IAggregateId AggregateId, Guid CorrelationId)> BuildExecutionContext(IEvent e)
		 => f => (f.EventHandler(e), new AggregateId(e.AggregateId.ToString()), Guid.Parse(e.Meta[nameof(EventMetaData.CorrelationId)]));

		static Func<(Type Type, EventHandle EventHandler), bool> DelegatefForTypeOrInterface(IEvent e)
			=> kv => kv.Type == e.GetType() || e.GetType().GetTypeInfo().GetInterfaces().Any(t => t == kv.Type);
	}


}
