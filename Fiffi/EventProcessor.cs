using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi
{
	public class EventProcessor
	{
		readonly CommandLocks _locks;
		readonly Action<string> _logger;
		readonly List<(Type Type, Func<IEvent, Task> EventHandler)> _handlers = new List<(Type, Func<IEvent, Task>)>();

		public EventProcessor() : this(new CommandLocks(), s => { })
		{ }

		public EventProcessor(CommandLocks locks) : this(locks, s => { })
		{ }

		public EventProcessor(CommandLocks locks, Action<string> logger)
		{
			_locks = locks;
			_logger = logger;
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
				.Select(ExecuteDelegate(e)))
				.ToArray();

			await Task.WhenAll(executionContext.Select(x => x.EventHandler));

			//Only release once per aggregate
			executionContext.GroupBy(x => x.CorrelationId)
			.Select(x => x.First())
			.ForEach(x => _locks.ReleaseIfPresent(x.AggregateId, x.CorrelationId, _logger));
		}

		static Func<(Type Type, Func<IEvent, Task> EventHandler), (Task EventHandler, IAggregateId AggregateId, Guid CorrelationId)> ExecuteDelegate(IEvent e)
		 => f => (f.EventHandler(e), new AggregateId(e.AggregateId.ToString()), Guid.Parse(e.Meta[nameof(EventMetaData.CorrelationId)]));

		static Func<(Type Type, Func<IEvent, Task> EventHandler), bool> DelegatefForTypeOrInterface(IEvent e)
			=> kv => kv.Type == e.GetType() || e.GetType().GetTypeInfo().GetInterfaces().Any(t => t == kv.Type);


	}

	internal class EventMetaData
	{
		internal static readonly object CorrelationId;
		internal static readonly object EventId;

	}
}
