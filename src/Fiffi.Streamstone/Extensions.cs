﻿using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Streamstone;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;



namespace Fiffi.Streamstone
{
	using TypeResolver = Func<string, Type>;

	public static class Extensions
	{
		public static async Task<long> GetVersionAsync(this Partition partiton)
		{
			var existent = await Stream.TryOpenAsync(partiton);
			return existent.Found
				? existent.Stream.Version
				: 0;
		}

		public static async Task<long> AppendToStreamAsync(this Partition partition, long version, IEvent[] events, Func<IEvent, global::Streamstone.EventData> mapper)
		{
			var existent = await Stream.TryOpenAsync(partition);
			var stream = existent.Found
				? existent.Stream
				: new Stream(partition);

			if (stream.Version != version)
				throw new DBConcurrencyException($"wrong version - expected {version} but was {stream.Version}");

			var r = await Stream.WriteAsync(partition, (int)version, events.Select(mapper).ToArray());
			return r.Stream.Version;
		}

        public static async Task<long> AppendToStreamAsync(this Partition partition, IEvent[] events)
        {
            var stream = new Stream(partition);
            var r = await Stream.WriteAsync(stream, events.Select(e => e.ToEventData()).ToArray());
            return r.Stream.Version;
        }

        public static Task<long> AppendToEventStreamAsync(this CloudTable table, string streamName, IEvent[] events)
            => new Partition(table, streamName).AppendToStreamAsync(events);

        public static async Task<long> AppendToEventStreamAsync(this CloudTable table, string streamName, long version, params IEvent[] events)
			=> await new Partition(table, streamName).AppendToStreamAsync(version, events, e => e.ToEventData());

		public static Task AppendWithSnapshotAsync(this CloudTable table, string streamName, long version, IEvent[] events, Include snapshot)
		=> new Partition(table, streamName).AppendToStreamAsync(version, events,
			e => e.ToEventData(snapshot));

		public static async Task<(IEnumerable<IEvent> Events, long Version)> ReadEventsFromStreamAsync(this CloudTable table, string streamName, TypeResolver typeResolver)
			=> await ReadFromStreamAsync(new Partition(table, streamName), typeResolver.ToEvent);

		public static async Task<(IEnumerable<IEvent> Events, long Version)> ReadEventsFromStreamAsync(this CloudTable table, string streamName, long fromVersion, TypeResolver typeResolver)
			=> await ReadFromStreamAsync(new Partition(table, streamName), typeResolver.ToEvent, fromVersion);

		//public static Task<T> GetSnapshotAsync<T>(this CloudTable table, string streamName, string rowKey)
		//where T : ITableEntity, new()
		//=> Task.FromResult(table.CreateQuery<T>()
		//	.Where(x => x.PartitionKey == streamName)
		//	.Where(x => x.RowKey == rowKey)
		//	.SingleOrDefault());

		static async Task<(IEnumerable<IEvent> Events, long Version)> ReadFromStreamAsync(Partition partition, Func<EventEntity, IEvent> mapper, long fromVersion = 1)
		{
			if (! await Stream.ExistsAsync(partition)) return (Enumerable.Empty<IEvent>(), 0);

			var events = new List<IEvent>();
			
			StreamSlice<EventEntity> slice;
			var nextSliceStart = (int)fromVersion;
			var version = fromVersion;

			do
			{
				slice = await Stream.ReadAsync<EventEntity>(partition, nextSliceStart);

				events.AddRange(slice.Events.Select(mapper));

				nextSliceStart = slice.HasEvents
					? slice.Events.Last().Version + 1
					: -1;

				if (slice.HasEvents)
					version = slice.Events.Last().Version;

			}
			while (!slice.IsEndOfStream);

			return (events, version);
		}

		public static IEvent ToEvent(this TypeResolver typeResolver, EventEntity e) 
			=> ToEvent(e.Data, typeResolver(e.Type))
				.Tap(evt => evt.Meta.AddStoreMetaData(new EventStoreMetaData { EventPosition = e.Version, EventVersion = e.Version }));

		//TODO move ?
		public static Type ToType(string typeValue)
		{
			var type = Type.GetType(typeValue);

			if (type == null)
				type = Assembly.GetEntryAssembly().GetType(typeValue);

			if (type == null)
				throw new InvalidOperationException($"Could not find type for event : {typeValue}");

			return type;
		}

		//TODO move ?
		public static IEvent ToEvent(string data, Type type) => (IEvent)JsonConvert.DeserializeObject(data, type);

		public static global::Streamstone.EventData ToEventData(this IEvent e, params Include[] includes)
		{
			var id = Guid.NewGuid();

			var properties = new
			{
				Id = e.Meta == null ? id : e.Meta.ContainsKey(nameof(EventMetaData.EventId)) ? Guid.Parse(e.Meta[nameof(EventMetaData.EventId)]) : id,
				Type = e.GetType().Name,
				Data = JsonConvert.SerializeObject(e),
				Offset = e.Meta == null ? -1 : e.Meta.ContainsKey("Offset") ? long.Parse(e.Meta["Offset"]) : -1,
				SequenceNumber = e.Meta == null ? -1 : e.Meta.ContainsKey("SequenceNumber") ? long.Parse(e.Meta["SequenceNumber"]) : -1
			};
			return new global::Streamstone.EventData(EventId.From(properties.Id), EventProperties.From(properties), includes.Any() ? EventIncludes.From(includes) : EventIncludes.None);
		}
	}
}
