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
		readonly Func<StorageEvent, Type> metaAccessor;
		readonly Func<EventData, IEvent> deserializer;


		public ReliableEventStore(IReliableStateManager reliableStateManager, Func<IEvent, EventData> serializer, Func<EventData, IEvent> deserializer)
			: this (reliableStateManager, null, serializer, deserializer)
		{ }

		public ReliableEventStore(IReliableStateManager reliableStateManager,
			ITransaction tx, Func<IEvent, EventData> serializer, Func<EventData, IEvent> deserializer)
		{
			this.stateManager = reliableStateManager;
			this.tx = tx;
			this.serializer = serializer;
			this.deserializer = deserializer;
		}

		public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events) =>
			tx == null
			?
			this.stateManager.AppendToStreamAsync(streamName, version, events, serializer)
			:
			this.stateManager.AppendToStreamAsync(tx, streamName, version, events, serializer);

		public Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(string streamName, int version) =>
			tx == null
			?
			this.stateManager.LoadEventStreamAsync(streamName, version, deserializer)
			:
			this.stateManager.LoadEventStreamAsync(tx, streamName, version, deserializer);
	}
}
