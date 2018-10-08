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
		private readonly IDictionary<IAggregateId, Tuple<Guid, SemaphoreSlim>> _locks;
		private readonly Action<string> _logger;
		private readonly List<Tuple<Type, Func<IEvent, Task>>> _h = new List<Tuple<Type, Func<IEvent, Task>>>();

		public EventProcessor() : this(new Dictionary<IAggregateId, Tuple<Guid, SemaphoreSlim>>(), s => { })
		{ }

		public EventProcessor(IDictionary<IAggregateId, Tuple<Guid, SemaphoreSlim>> locks) : this(locks, s => { })
		{ }

		public EventProcessor(IDictionary<IAggregateId, Tuple<Guid, SemaphoreSlim>> locks, Action<string> logger)
		{
			_locks = locks;
			_logger = logger;
		}


		public void Register<T>(Func<T, Task> f)
			where T : IEvent
			=> _h.Add(new Tuple<Type, Func<IEvent, Task>>(typeof(T), @event => f((T)@event)));


		public async Task PublishAsync(params IEvent[] events)
		{
			if (!events.All(e => e.Meta.ContainsKey(nameof(EventMetaData.CorrelationId))))
				throw new ArgumentException("CorrelationId required");

			var h = events.SelectMany(e => _h
				.Where(DelegatefForTypeOrInterface(e))
				.Select(ExecuteDelegate(e)))
				.ToArray();

			await Task.WhenAll(h.Select(x => x.Item1));

			//Only release once per aggregate
			h.GroupBy(x => x.Item3)
			.Select(x => x.First())
			.ForEach(x => ReleaseIfPresent(_locks, x.Item2, x.Item3, _logger));
		}

		private static Func<Tuple<Type, Func<IEvent, Task>>, Tuple<Task, IAggregateId, Guid>> ExecuteDelegate(IEvent e)
		 => f => new Tuple<Task, IAggregateId, Guid>(f.Item2(e), new AggregateId(e.AggregateId.ToString()), Guid.Parse(e.Meta[nameof(EventMetaData.CorrelationId)]));

		private static Func<Tuple<Type, Func<IEvent, Task>>, bool> DelegatefForTypeOrInterface(IEvent e)
			=> kv => kv.Item1 == e.GetType() || e.GetType().GetTypeInfo().GetInterfaces().Any(t => t == kv.Item1);

		private static void ReleaseIfPresent(IDictionary<IAggregateId, Tuple<Guid, SemaphoreSlim>> locks,
			IAggregateId aggregateId, Guid correlationId, Action<string> logger)
		{
			if (!locks.ContainsKey(aggregateId))
			{
				logger($"Lock for {aggregateId.Id} not found.");
				return;
			}

			if (locks[aggregateId].Item1 != correlationId)
			{
				logger($"Lock for {aggregateId.Id} with correlation {correlationId} not found.");
				return;
			}

			logger($"Lock for {aggregateId.Id} with correlation {correlationId} about to release.");
			locks[aggregateId].Item2.Release();
			logger($"Lock for {aggregateId.Id} with correlation {correlationId} released.");

		}
	}

	internal class EventMetaData
	{
		internal static readonly object CorrelationId;
		internal static readonly object EventId;

	}
}
