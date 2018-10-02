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
				var streams = await stateManager.GetOrAddAsync<IReliableDictionary<string, List<StorageEvent>>>(tx, streamsName);
				var result = await streams.AppendToStreamAsync(tx, streamName, version, events);
				var queue = await stateManager.GetOrAddAsync<IReliableQueue<StorageEvent>>(tx, queueName);
				await Task.WhenAll(result.Item1.Select(e => queue.EnqueueAsync(tx, e)));
				await tx.CommitAsync();
				return result.Item2;
			}
		}

		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableStateManager stateManager, string streamName, int version)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				var streams = await stateManager.GetOrAddAsync<IReliableDictionary<string, List<StorageEvent>>>(tx, "streams");
				var result = await streams.LoadEventStreamAsync(tx, streamName, version);
				await tx.CommitAsync();
				return result;
			}
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

		public static async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(this IReliableDictionary<string, List<StorageEvent>> streams, ITransaction tx, string streamName, int version)
		{
			var streamResult = await streams.TryGetValueAsync(tx, streamName);

			if (!streamResult.HasValue)
			{
				return (Enumerable.Empty<IEvent>(), default(long));
			}

			var stream = streamResult.Value;

			return (stream.Skip(version).Select(x => blaj(x)), stream.Count);
		}

		public static async Task DequeueAsync(this IReliableStateManager stateManager, Func<StorageEvent, Task> action, CancellationToken cancellationToken, string queueName = defaultQueueName)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				var queue = await stateManager.GetOrAddAsync<IReliableQueue<StorageEvent>>(tx, queueName);
				var result = await queue.TryDequeueAsync(tx, TimeSpan.FromSeconds(3), cancellationToken);
				if (result.HasValue)
				{
					await action(result.Value);
					await tx.CommitAsync();
				}
			}
		}

		static EventData Map(IEvent e) => new EventData(Guid.Empty, null, null);

		static IEvent blaj(StorageEvent storageEvent) => (IEvent)null;
	}
}
