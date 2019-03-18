using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class ServiceFabricEventStoreExtensions
	{
		const string defaultOutBoxQueueName = "outbox";
		const string defaultInboxQueueName = "inbox";
		const string defaultStreamsName = "streams";

		public static Task<long> AppendToStreamAsync(this IReliableStateManager stateManager,
			string streamName, long version, IEvent[] events,
			string queueName = defaultOutBoxQueueName, string streamsName = defaultStreamsName) =>
			stateManager.AppendToStreamAsync(streamName, version, events, Serialization.FabricSerialization(), queueName, streamsName);

		public static async Task<long> AppendToStreamAsync(this IReliableStateManager stateManager,
			string streamName, long version, IEvent[] events, Func<IEvent, EventData> serializer,
			string queueName = defaultOutBoxQueueName, string streamsName = defaultStreamsName)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await stateManager.AppendAndMapToStreamAsync(tx, streamName, version, events, serializer, streamsName);
				await stateManager.EnqueuAsync(tx, result.Item1.Select(x => x.ToEventData()), queueName);
				await tx.CommitAsync();
				return result.Item2;
			}
		}

		public static async Task<long> AppendToStreamAsync(this IReliableStateManager stateManager,
			ITransaction tx, string streamName, long version, IEvent[] events, Func<IEvent, EventData> serializer, string streamsName = defaultStreamsName)
			=> (await stateManager.AppendAndMapToStreamAsync(tx, streamName, version, events, serializer, streamsName)).Item2;

		static async Task<(IEnumerable<StorageEvent>, long)> AppendAndMapToStreamAsync(this IReliableStateManager stateManager,
			ITransaction tx, string streamName, long version, IEvent[] events, Func<IEvent, EventData> serializer, string streamsName = defaultStreamsName)
		{
			var streams = await stateManager.GetOrAddAsync<IReliableDictionary<string, List<StorageEvent>>>(tx, streamsName);
			var result = await streams.AppendToStreamAsync(tx, streamName, version, events, serializer);
			return result;
		}

		public static async Task<(IEnumerable<StorageEvent>, long)> AppendToStreamAsync(this IReliableDictionary<string, List<StorageEvent>> streams,
			ITransaction tx, string streamName, long version, IEvent[] events, Func<IEvent, EventData> serializer)
		{
			if (!events.Any())
				return (Enumerable.Empty<StorageEvent>(), version);

			var storageEvents = events.Select((x, i) => new StorageEvent(streamName, serializer(x), (int)(version + i + 1)));

			var streamResult = await streams.TryGetValueAsync(tx, streamName);
			if (!streamResult.HasValue)
			{
				await streams.AddAsync(tx, streamName, new List<StorageEvent>(storageEvents));
				return (storageEvents, (long)storageEvents.Last().EventNumber);
			}

			var stream = streamResult.Value;

			var duplicateEvent = stream.FirstOrDefault(se => events.Any(e => se.EventId == e.EventId()));
			if (duplicateEvent != null)
				throw new DuplicateNameException($"Event with id {duplicateEvent.EventId} already in stream {streamName}");

			if (version != stream.Count)
			{
				throw new DBConcurrencyException($"Concurrency conflict when appending to stream {streamName}. Expected revision {version} : Actual revision {stream.Count}");
			}

			stream.AddRange(storageEvents);
			return (storageEvents, (long)storageEvents.Last().EventNumber);
		}

		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableStateManager stateManager, string streamName,
			int version, Func<EventData, IEvent> deserializer, string streamsName = defaultStreamsName)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await stateManager.LoadEventStreamAsync(tx, streamName, version, deserializer, streamsName);
				await tx.CommitAsync();
				return result;
			}
		}

		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableStateManager stateManager,
			ITransaction tx, string streamName, int version, Func<EventData, IEvent> deserializer, string streamsName = defaultStreamsName)
		{
			//var streams = await stateManager.GetOrAddAsync<IReliableDictionary<string, List<StorageEvent>>>(tx, streamsName);
			//https://github.com/Azure/service-fabric-issues/issues/24
			var streams = await stateManager.GetOrAddAsync<IReliableDictionary<string, List<StorageEvent>>>(streamsName);
			var result = await streams.LoadEventStreamAsync(tx, streamName, version, deserializer);
			return result;
		}

		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableDictionary<string, List<StorageEvent>> streams,
			ITransaction tx, string streamName, int version, Func<EventData, IEvent> deserializer)
		{
			var streamResult = await streams.TryGetValueAsync(tx, streamName);

			if (!streamResult.HasValue)
			{
				return (Enumerable.Empty<IEvent>(), default(long));
			}

			var stream = streamResult.Value;

			return (stream.Skip(version).Select(x => x.ToEventData().ToEvent(deserializer)), stream.Count);
		}

		//TODO move to queue etx
		public static Task EnqueuAsync(this IReliableStateManager stateManager, IEvent @event, Func<IEvent, EventData> serialzer, string queueName)
			=> stateManager.UseTransactionAsync(tx => stateManager.EnqueuAsync(tx, new[] { @event }, serialzer, queueName));

		public static Task EnqueuAsync(this IReliableStateManager stateManager, ITransaction tx, IEvent @event, Func<IEvent, EventData> serializer, string queueName)
			=> stateManager.EnqueuAsync(tx, new[] { @event }, serializer, queueName);

		public static Task EnqueuAsync<T>(this IReliableStateManager stateManager, IEnumerable<T> events, Func<T, EventData> serialzer, string queueName = defaultOutBoxQueueName)
			where T : IEvent
			=> stateManager.UseTransactionAsync(tx => stateManager.EnqueuAsync(tx, events, serialzer, queueName));

		public static Task EnqueuAsync<T>(this IReliableStateManager stateManager, ITransaction tx, IEnumerable<T> events, Func<T, EventData> serializer, string queueName = defaultOutBoxQueueName)
			where T : IEvent
			=> stateManager.EnqueuAsync(tx, events.Select(x => serializer(x)), queueName);

		public static async Task EnqueuAsync(this IReliableStateManager stateManager, ITransaction tx,
			IEnumerable<EventData> events, string queueName = defaultOutBoxQueueName)
		{
			var queue = await stateManager.GetOrAddAsync<IReliableQueue<EventData>>(queueName);
			await Task.WhenAll(events.Select(e => queue.EnqueueAsync(tx, e)));
		}

		public static Task DequeueAsync<T>(this IReliableStateManager stateManager, Func<T, Task> action,
			Func<EventData, T> deserializer,
			CancellationToken cancellationToken, string queueName = defaultOutBoxQueueName)
			=> stateManager.UseTransactionAsync(tx => stateManager.DequeueAsync(tx, action, deserializer, cancellationToken, true, queueName), autoCommit: false);

		public static async Task DequeueAsync<T>(this IReliableStateManager stateManager, ITransaction tx, Func<T, Task> action,
			 Func<EventData, T> deserializer, CancellationToken cancellationToken, bool commitOnAction = false, string queueName = defaultOutBoxQueueName)
		{
			if (deserializer == null)
				throw new ArgumentNullException(nameof(deserializer));
			var queue = await stateManager.GetOrAddAsync<IReliableQueue<EventData>>(tx, queueName);
			var result = await queue.TryDequeueAsync(tx, TimeSpan.FromSeconds(3), cancellationToken);
			if (result.HasValue)
			{
				await action(deserializer(result.Value));
				if (commitOnAction) await tx.CommitAsync();
			}
		}
	}
}
