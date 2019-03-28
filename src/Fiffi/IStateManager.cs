using System;
using System.Threading.Tasks;

namespace Fiffi
{
	public interface IStateManager
	{
		Task<T> GetAsync<T>(IAggregateId id);

		Task SaveAsync<T>(IAggregateId aggregateId, T state, IEvent[] outboxEvents);

		Func<IEvent[], Task> OnPublish(Func<IEvent[], Task> publish);

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Call OnPublish while publishing events</remarks>
		/// <returns></returns>
		Task PublishAllUnPublishedEventsAsync(Func<IEvent[], Task> publish);
	}
}
