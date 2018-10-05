using Microsoft.ServiceFabric.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public class ReliableEventStore : IEventStore
	{
		readonly IReliableStateManager stateManager;
		readonly ITransaction tx;

		public ReliableEventStore(IReliableStateManager reliableStateManager) : this(reliableStateManager, null)
		{ }


		public ReliableEventStore(IReliableStateManager reliableStateManager, ITransaction tx)
		{
			this.stateManager = reliableStateManager;
			this.tx = tx;
		}

		public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events) =>
			tx == null
			?
			this.stateManager.AppendToStreamAsync(streamName, version, events)
			:
			this.stateManager.AppendToStreamAsync(tx, streamName, version, events);


		public Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(string streamName, int version) =>
			tx == null
			?
			this.stateManager.LoadEventStreamAsync(streamName, version)
			:
			this.stateManager.LoadEventStreamAsync(tx, streamName, version);
	}
}
