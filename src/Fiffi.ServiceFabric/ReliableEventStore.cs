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

		public ReliableEventStore(IReliableStateManager reliableStateManager, 
			Func<string, Type> typeResolver,
			Func<IEvent, EventData> serializer) 
			: this(reliableStateManager, null, typeResolver, serializer)
		{ }

		readonly Func<string, Type> typeResolver;
		readonly Func<IEvent, EventData> serializer;

		public ReliableEventStore(IReliableStateManager reliableStateManager, 
			ITransaction tx, Func<string, Type> typeResolver, Func<IEvent, EventData> serializer)
		{
			this.serializer = serializer;
			this.stateManager = reliableStateManager;
			this.tx = tx;
			this.typeResolver = typeResolver;
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
			this.stateManager.LoadEventStreamAsync(streamName, version, typeResolver)
			:
			this.stateManager.LoadEventStreamAsync(tx, streamName, version, typeResolver);
	}
}
