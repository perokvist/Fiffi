using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;



//TODO remove ??
//namespace Fiffi.ServiceFabric
//{
	//public class PublishingReliableEventStore : IEventStore
	//{
	//	readonly IReliableStateManager stateManager;
	//	readonly Func<IEvent, EventData> serializer;
	//	readonly Func<EventData, IEvent> deserializer;
	//	public PublishingReliableEventStore(IReliableStateManager reliableStateManager, Func<IEvent, EventData> serializer, Func<EventData, IEvent> deserializer)
	//	{
	//		this.stateManager = reliableStateManager;
	//		this.serializer = serializer;
	//		this.deserializer = deserializer;
	//	}

	//	public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events) =>
	//		this.stateManager.AppendToStreamAsync(streamName, version, events, serializer);

	//	public Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(string streamName, int version) =>
	//		this.stateManager.LoadEventStreamAsync(streamName, version, deserializer);
	//}
//}
