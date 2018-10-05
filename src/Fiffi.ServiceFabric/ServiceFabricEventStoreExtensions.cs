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
		const string defaultQueueName = "outbox";
		const string defaultStreamsName = "streams";


		public static async Task<long> AppendToStreamAsync(this IReliableStateManager stateManager, string streamName, long version, IEvent[] events, string queueName = defaultQueueName, string streamsName = defaultStreamsName)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await stateManager.AppendAndMapToStreamAsync(tx, streamName, version, events, streamsName);
				await stateManager.EnqueuAsync(tx, result.Item1, queueName);
				await tx.CommitAsync();
				return result.Item2;
			}
		}

		public static async Task<long> AppendToStreamAsync(this IReliableStateManager stateManager, ITransaction tx, string streamName, long version, IEvent[] events, string streamsName = defaultStreamsName)
			=> (await stateManager.AppendAndMapToStreamAsync(tx, streamName, version, events, streamsName)).Item2;

		static async Task<(IEnumerable<StorageEvent>, long)> AppendAndMapToStreamAsync(this IReliableStateManager stateManager, ITransaction tx, string streamName, long version, IEvent[] events, string streamsName = defaultStreamsName)
		{
			var streams = await stateManager.GetOrAddAsync<IReliableDictionary<string, List<StorageEvent>>>(tx, streamsName);
			var result = await streams.AppendToStreamAsync(tx, streamName, version, events);
			return result;
		}

		public static async Task<(IEnumerable<StorageEvent>, long)> AppendToStreamAsync(this IReliableDictionary<string, List<StorageEvent>> streams, ITransaction tx, string streamName, long version, IEvent[] events)
		{
			var storageEvents = events.Select((x, i) => new StorageEvent(streamName, Map(x), (int)(version + i + 1)));

			var streamResult = await streams.TryGetValueAsync(tx, streamName);
			if (!streamResult.HasValue)
			{
				await streams.AddAsync(tx, streamName, new List<StorageEvent>(storageEvents));
				return (storageEvents, (long)storageEvents.Last().EventNumber);
			}

			var stream = streamResult.Value;

			if (version != stream.Count)
			{
				throw new DBConcurrencyException($"Concurrency conflict when appending to stream {streamName}. Expected revision {version} : Actual revision {stream.Count}");
			}

			stream.AddRange(storageEvents);
			return (storageEvents, (long)storageEvents.Last().EventNumber);
		}


		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableStateManager stateManager, string streamName, int version, string streamsName = defaultStreamsName)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				var result = await stateManager.LoadEventStreamAsync(tx, streamName, version, streamsName);
				await tx.CommitAsync();
				return result;
			}
		}

		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableStateManager stateManager, ITransaction tx, string streamName, int version, string streamsName = defaultStreamsName)
		{
			var streams = await stateManager.GetOrAddAsync<IReliableDictionary<string, List<StorageEvent>>>(tx, streamsName);
			var result = await streams.LoadEventStreamAsync(tx, streamName, version);
			return result;
		}

		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableDictionary<string, List<StorageEvent>> streams, ITransaction tx, string streamName, int version)
		{
			var streamResult = await streams.TryGetValueAsync(tx, streamName);

			if (!streamResult.HasValue)
			{
				return (Enumerable.Empty<IEvent>(), default(long));
			}

			var stream = streamResult.Value;

			return (stream.Skip(version).Select(x => ToEvent(x)), stream.Count);
		}

		public static Task EnqueuAsync<T>(this IReliableStateManager stateManager, IEnumerable<T> events, string queueName = defaultQueueName)
			=> stateManager.UseTransactionAsync(tx => stateManager.EnqueuAsync(tx, events, queueName));

		public static async Task EnqueuAsync<T>(this IReliableStateManager stateManager, ITransaction tx, IEnumerable<T> events, string queueName = defaultQueueName)
		{
			var queue = await stateManager.GetOrAddAsync<IReliableQueue<T>>(tx, queueName);
			await Task.WhenAll(events.Select(e => queue.EnqueueAsync(tx, e)));
		}

		public static Task DequeueAsync<T>(this IReliableStateManager stateManager, Func<T, Task> action, CancellationToken cancellationToken, string queueName = defaultQueueName)
			=> stateManager.UseTransactionAsync(tx => stateManager.DequeueAsync(tx, action, cancellationToken, true, queueName), autoCommit: false);

		public static async Task DequeueAsync<T>(this IReliableStateManager stateManager, ITransaction tx, Func<T, Task> action, CancellationToken cancellationToken, bool commitOnAction = false, string queueName = defaultQueueName)
		{
			var queue = await stateManager.GetOrAddAsync<IReliableQueue<T>>(tx, queueName);
			var result = await queue.TryDequeueAsync(tx, TimeSpan.FromSeconds(3), cancellationToken);
			if (result.HasValue)
			{
				await action(result.Value);
				if(commitOnAction) await tx.CommitAsync();
			}
		}

		//TODO custom event serialization ?
		static EventData Map(IEvent e) => new EventData(e.EventId(), e, e.Meta);

		static Guid EventId(this IEvent e) => Guid.Parse(e.Meta["eventId"]);

		static IEvent ToEvent(StorageEvent storageEvent) => (IEvent)storageEvent.EventBody;
	}
}
