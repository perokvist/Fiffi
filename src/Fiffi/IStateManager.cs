using System;
using System.Threading.Tasks;

namespace Fiffi
{
	public interface IStateManager
	{
		Task<T> GetAsync<T>(IAggregateId id);

		/// <remarks>Expects that state and outbox events is persisted in the same transaction</remarks>
		Task SaveAsync<T>(IAggregateId aggregateId, T state, IEvent[] outboxEvents);

		Func<IEvent[], Task> OnPublish(Func<IEvent[], Task> publish);

		Task<IEvent[]> GetAllUnPublishedEvents();
	}
}
