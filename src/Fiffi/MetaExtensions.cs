using System;
using System.Collections.Generic;

namespace Fiffi
{
	public static class MetaExtensions
	{
		public static void AddTypeInfo(this IDictionary<string, string> meta, IEvent e)
		{
			var t = e.GetType();
			var typeProperties = new Dictionary<string, string>
			{
				{ "name", t.Name },
				{ "eventname", t.Name },
				{ "type.name", t.Name },
				{ "type.assemblyqualifiedname", t.AssemblyQualifiedName },
				{ "type.fullname", t.FullName },
				{ "type.version", e.GetType().Assembly.GetName().Version.ToString() }
			};
			typeProperties.ForEach(x => meta.TryAdd(x.Key, x.Value));
		}

		public static string GetStreamName(this IEvent @event) => @event.Require(nameof(EventMetaData.StreamName));

		public static string GetAggregateName(this IEvent @event) => @event.Require(nameof(EventMetaData.AggregateName));

		public static long GetVersion(this IEvent @event) => long.Parse(@event.Require(nameof(EventMetaData.Version)));

        public static Guid EventId(this IEvent e) => Guid.Parse(e.Require(nameof(EventMetaData.EventId)));

		public static bool HasCorrelation(this IEvent @event) => @event.HasMeta(nameof(EventMetaData.CorrelationId));

        public static string GetEventName(this IEvent e)
            => e.Require("eventname");

        public static Type GetEventType(this IDictionary<string, string> meta, Func<string, Type> f)
			=> f(meta["type.name"]);

		public static Guid GetCorrelation(this IEvent @event) => Guid.Parse(@event.Require(nameof(EventMetaData.CorrelationId)));

		public static string GetTrigger(this IEvent @event) => @event.Require(nameof(EventMetaData.TriggeredBy));

		public static int GetTriggerId(this IEvent @event) => int.Parse(@event.Require(nameof(EventMetaData.TriggeredById)));

		public static DateTime OccuredAt(this IEvent @event) => new DateTime(long.Parse(@event.Require(nameof(EventMetaData.OccuredAt))));

		internal static string Require(this IEvent @event, string keyName)
			=> @event.Meta.ContainsKey(keyName.ToLower()) ? @event.Meta[keyName.ToLower()] : throw new ArgumentException($"{keyName.ToLower()} for {@event.GetType()} required");
		//TODO switch case for handling meta == null (when testing)

		public static string GetMetaOrDefault<T>(this IDictionary<string, string> meta, string keyName, T @default = default(T))
			=> meta.ContainsKey(keyName.ToLower()) ? meta[keyName.ToLower()] : @default.ToString();

		public static bool HasMeta(this IEvent @event, string keyName)
			=> @event.Meta.ContainsKey(keyName.ToLower());

		public static EventMetaData GetEventMetaData(this IDictionary<string, string> meta)
		=> new EventMetaData {
			AggregateName = meta.GetMetaOrDefault<string>(nameof(EventMetaData.AggregateName)),
			CorrelationId = Guid.Parse(meta.GetMetaOrDefault<Guid>(nameof(EventMetaData.CorrelationId))),
            CausationId = Guid.Parse(meta.GetMetaOrDefault<Guid>(nameof(EventMetaData.CausationId))),
            EventId = Guid.Parse(meta.GetMetaOrDefault<Guid>(nameof(EventMetaData.EventId))),
			OccuredAt = long.Parse(meta.GetMetaOrDefault<long>(nameof(EventMetaData.OccuredAt))),
			StreamName = meta.GetMetaOrDefault<string>(nameof(EventMetaData.StreamName)),
			TriggeredBy = meta.GetMetaOrDefault<string>(nameof(EventMetaData.TriggeredBy)),
			Version = long.Parse(meta.GetMetaOrDefault<long>(nameof(EventMetaData.Version))),
		};


		public static void AddMetaData(this IDictionary<string, string> meta, long newVersion, string streamName, string aggregateName, ICommand command, long occuredAt = default(long))
		=> meta.AddMetaData(new EventMetaData
		{
			AggregateName = aggregateName,
			CorrelationId = command.CorrelationId,
            CausationId = command.CorrelationId,
			EventId = Guid.NewGuid(),
			OccuredAt = occuredAt == default(long) ? DateTime.UtcNow.Ticks : occuredAt,
			StreamName = streamName,
			TriggeredBy = command.GetType().Name,
			TriggeredById = command.GetHashCode(),
			Version = newVersion
		});

		public static void AddMetaData(this IDictionary<string, string> meta, EventMetaData metaData)
		{
			meta[nameof(EventMetaData.Version).ToLower()] = metaData.Version.ToString();
			meta[nameof(EventMetaData.StreamName).ToLower()] = metaData.StreamName;
			meta[nameof(EventMetaData.AggregateName).ToLower()] = metaData.AggregateName;
			meta[nameof(EventMetaData.EventId).ToLower()] = metaData.EventId.ToString();
			meta[nameof(EventMetaData.CorrelationId).ToLower()] = metaData.CorrelationId.ToString();
            meta[nameof(EventMetaData.CausationId).ToLower()] = metaData.CausationId.ToString();
            meta[nameof(EventMetaData.TriggeredBy).ToLower()] = metaData.TriggeredBy;
			meta[nameof(EventMetaData.TriggeredById).ToLower()] = metaData.TriggeredById.ToString();
			meta[nameof(EventMetaData.OccuredAt).ToLower()] = metaData.OccuredAt.ToString();
		}
	}

	public class EventMetaData
	{
		public Guid CorrelationId { get; set; }
        public Guid CausationId { get; set; }
        public Guid EventId { get; set; }
		public string StreamName { get; set; }
		public string AggregateName { get; set; }
		public long Version { get; set; }
		public string TriggeredBy { get; set; }
		public long OccuredAt { get; set; }
		public int TriggeredById { get; set; }
	}
}
