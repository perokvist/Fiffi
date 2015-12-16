using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class ApplicationService
	{
		public static async Task Execute<TAggregate>(
			ICommand command,
			Func<TAggregate> factory,
			Func<TAggregate, IEnumerable<IEvent>> executeUsingThis,
			Func<IEvent[],Task> pub,
			IDictionary<Guid,Tuple<Guid, SemaphoreSlim>> locks
			)
		{
			
			if (!locks.ContainsKey(command.AggregateId))
				locks.Add(command.AggregateId, new Tuple<Guid, SemaphoreSlim>(command.CorrelationId, new SemaphoreSlim(1)));

			var @lock = locks[command.AggregateId];
			locks[command.AggregateId] = new Tuple<Guid, SemaphoreSlim>(command.AggregateId, @lock.Item2);

			await @lock.Item2.WaitAsync(TimeSpan.FromSeconds(5));

			await Execute(factory, executeUsingThis, pub, command.CorrelationId);
		}

		public static async Task Execute<TAggregate>(Func<TAggregate> factory, 
			Func<TAggregate, IEnumerable<IEvent>> executeUsingThis, 
			Func<IEvent[], Task> pub, Guid correlationId)
			=> await pub(executeUsingThis(factory()).Tap(x => AddMetaData(x, correlationId)).ToArray());

		private static IEnumerable<IEvent> AddMetaData(IEnumerable<IEvent> events, Guid correlationId)
			=> events.Select(e => e.With(nameof(IEvent.CorrelationId), correlationId));

	}
}