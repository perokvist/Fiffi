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

		public static string GetStreamName(this IEvent @event) => @event.Require(nameof(EventMetaData.StreamName).ToLower());

		public static long GetVersion(this IEvent @event) => long.Parse(@event.Require(nameof(EventMetaData.Version).ToLower()));

		public static Guid EventId(this IEvent e) => Guid.Parse(e.Require(nameof(EventMetaData.EventId).ToLower()));

		public static bool HasCorrelation(this IEvent @event) => @event.HasMeta(nameof(EventMetaData.CorrelationId));

		public static Type GetEventType(this IDictionary<string, string> meta, Func<string, Type> f)
			=> f(meta["type.name"]);

		public static Guid GetCorrelation(this IEvent @event) => Guid.Parse(@event.Require(nameof(EventMetaData.CorrelationId)));

		public static string GetTrigger(this IEvent @event) => @event.Require(nameof(EventMetaData.TriggeredBy));

		public static DateTime OccuredAt(this IEvent @event) => new DateTime(long.Parse(@event.Require(nameof(EventMetaData.OccuredAt))));

		internal static string Require(this IEvent @event, string keyName)
			=> @event.Meta.ContainsKey(keyName.ToLower()) ? @event.Meta[keyName.ToLower()] : throw new ArgumentException($"{keyName} for {@event.GetType()} required");

		public static string GetMetaOrDefault<T>(this IEvent @event, string keyName, string @default)
			=> @event.Meta.ContainsKey(keyName.ToLower()) ? @event.Meta[keyName.ToLower()] : @default;

		public static bool HasMeta(this IEvent @event, string keyName)
			=> @event.Meta.ContainsKey(keyName.ToLower()); 

		public static void AddMetaData(this IDictionary<string, string> meta , long newVersion, string streamName, string aggregateName, ICommand command)
		{
			meta[nameof(EventMetaData.Version).ToLower()] = newVersion.ToString();
			meta[nameof(EventMetaData.StreamName).ToLower()] = streamName;
			meta[nameof(EventMetaData.AggregateName).ToLower()] = aggregateName;
			meta[nameof(EventMetaData.EventId).ToLower()] = Guid.NewGuid().ToString();
			meta[nameof(EventMetaData.CorrelationId).ToLower()] = command.CorrelationId.ToString();
			meta[nameof(EventMetaData.TriggeredBy).ToLower()] = command.GetType().Name;
			meta[nameof(EventMetaData.OccuredAt).ToLower()] = DateTime.UtcNow.Ticks.ToString();
		}
	}

	class EventMetaData
	{
		internal static readonly object CorrelationId;
		internal static readonly object EventId;
		internal static readonly object StreamName;
		internal static readonly object AggregateName;
		internal static readonly object Version;
		internal static readonly object TriggeredBy;
		internal static readonly object OccuredAt;
	}
}
