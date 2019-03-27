using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public class ReliableEventStore : IEventStore
	{
		readonly IReliableStateManager stateManager;
		readonly ITransaction tx;
		readonly Func<IEvent, EventData> serializer;
		readonly Func<EventData, IEvent> deserializer;
		readonly Func<string, string> reliableCollectionNameProvider;

		public ReliableEventStore(IReliableStateManager reliableStateManager,
			ITransaction tx, Func<IEvent, EventData> serializer, Func<EventData, IEvent> deserializer)
			: this(reliableStateManager, tx, serializer, deserializer, NameByAggregate)
		{ }

		public ReliableEventStore(IReliableStateManager reliableStateManager,
			ITransaction tx, Func<IEvent, EventData> serializer, Func<EventData, IEvent> deserializer,
			Func<string, string> reliableCollectionNameProvider)
		{
			this.stateManager = reliableStateManager;
			this.tx = tx;
			this.serializer = serializer;
			this.deserializer = deserializer;
			this.reliableCollectionNameProvider = reliableCollectionNameProvider;
		}

		public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events) =>
			this.reliableCollectionNameProvider(streamName)
				.Pipe(name => name == null ? this.stateManager.AppendToStreamAsync(tx, streamName, version, events, serializer) : this.stateManager.AppendToStreamAsync(tx, streamName, version, events, serializer, name));

		public Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(string streamName, int version) =>
			this.reliableCollectionNameProvider(streamName)
			.Pipe(name => name == null ? this.stateManager.LoadEventStreamAsync(tx, streamName, version, deserializer) : this.stateManager.LoadEventStreamAsync(tx, streamName, version, deserializer, name));

		public static string NameByAggregate(string streamName)
		{
			if (streamName.Contains('-'))
				return streamName.Split('-')[0];

			return null;
		}
	}
}
