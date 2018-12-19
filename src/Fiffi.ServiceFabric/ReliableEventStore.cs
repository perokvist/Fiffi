using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public class ReliableEventStore : IEventStore
	{
		readonly IReliableStateManager stateManager;
		readonly ITransaction tx;
		readonly Func<IEvent, EventData> serializer;
		readonly Func<EventData, IEvent> deserializer;

		public ReliableEventStore(IReliableStateManager reliableStateManager,
			ITransaction tx, Func<IEvent, EventData> serializer, Func<EventData, IEvent> deserializer)
		{
			this.stateManager = reliableStateManager;
			this.tx = tx;
			this.serializer = serializer;
			this.deserializer = deserializer;
		}

		public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events) =>
			this.stateManager.AppendToStreamAsync(tx, streamName, version, events, serializer);

		public Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(string streamName, int version) =>
			this.stateManager.LoadEventStreamAsync(tx, streamName, version, deserializer);
	}
}
