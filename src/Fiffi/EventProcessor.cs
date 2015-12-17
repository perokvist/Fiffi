using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fiffi
{
	public class EventProcessor
	{
		private readonly IDictionary<Guid, Tuple<Guid, SemaphoreSlim>> _locks;
		private readonly List<Tuple<Type, Func<IEvent, Task>>> _h = new List<Tuple<Type, Func<IEvent, Task>>>();

		public EventProcessor(IDictionary<Guid, Tuple<Guid, SemaphoreSlim>> locks, ILogger logger)
		{
			_locks = locks;
		}

		public void Register<T>(Func<T, Task> f)
			where T : IEvent
			=> _h.Add(new Tuple<Type, Func<IEvent, Task>>(typeof(T), @event => f((T)@event)));


		public async Task PublishAsync(params IEvent[] events)
		{
			var h = events.SelectMany(e => _h
				.Where(kv => kv.Item1 == e.GetType() || e.GetType().GetInterfaces().Any(t => t == kv.Item1))
				.Select(f => new Tuple<Task, Guid, Guid>(f.Item2(e), e.AggregateId, e.CorrelationId)))
				.ToArray();

			await Task.WhenAll(h.Select(x => x.Item1));

			h.ForEach(x => ReleaseIfPresent(_locks, x.Item2, x.Item3));
		}

		private static void ReleaseIfPresent(IDictionary<Guid, Tuple<Guid, SemaphoreSlim>> locks,
			Guid aggregateId, Guid correlationId)
		{
			if (!locks.ContainsKey(aggregateId)) return;

			if (locks[aggregateId].Item1 == correlationId)
				locks[aggregateId].Item2.Release();
		}
	}
}