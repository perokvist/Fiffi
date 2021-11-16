using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi;

public static class MetaExtensions
{
    public static void AddTypeInfo(this IDictionary<string, string> meta, IEvent e)
    {
        var envelopeType = e.GetType();
        var eventType = e.Event.GetType();

        if (meta == null)
            throw new ArgumentException("Meta dictionary cannot be null. When adding type info");

        var typeProperties = new Dictionary<string, string>
            {
                { "name", eventType.Name },
                { "eventname", eventType.Name },
                { "type.name", eventType.Name },
                { "type.assemblyqualifiedname", eventType.AssemblyQualifiedName },
                { "type.fullname", eventType.FullName },
                { "type.version", eventType.Assembly.GetName().Version.ToString() }
            };

        typeProperties.ForEach(x => meta.TryAdd(x.Key, x.Value));
    }

    public static string GetStreamName(this IEvent @event) => @event.Require(nameof(EventMetaData.StreamName));

    public static string GetAggregateName(this IEvent @event) => @event.Require(nameof(EventMetaData.AggregateName));

    public static Guid EventId(this IEvent e) => Guid.Parse(e.Require(nameof(EventMetaData.EventId)));

    public static bool HasCorrelation(this IEvent @event) => @event.HasMeta(nameof(EventMetaData.CorrelationId));

    public static string GetEventName(this IEvent e)
        => e.Require("eventname");

    public static Type GetEventType(this IDictionary<string, string> meta, Func<string, Type> f)
        => f(meta["type.name"]);

    public static Guid GetCorrelation(this IEvent @event) => Guid.Parse(@event.Require(nameof(EventMetaData.CorrelationId)));

    public static string GetTrigger(this IEvent @event) => @event.Require(nameof(EventMetaData.TriggeredBy));

    public static Guid GetCausationId(this IEvent @event) => Guid.Parse(@event.Require(nameof(EventMetaData.CausationId)));

    public static long GetBasedOnStreamVersion(this IEvent @event) => long.Parse(@event.Require(nameof(EventMetaData.BasedOnStreamVersion)));

    public static DateTime OccuredAt(this IEvent @event) => new DateTime(long.Parse(@event.Require(nameof(EventMetaData.OccuredAt))));

    public static long OccuredAtTicks(this IEvent @event) => long.Parse(@event.Require(nameof(EventMetaData.OccuredAt)));

    internal static string Require(this IEvent @event, string keyName)
        => @event.Meta.ContainsKey(keyName.ToLower()) ? @event.Meta[keyName.ToLower()] : throw new ArgumentException($"{keyName.ToLower()} for {@event.GetType()} required");
    //TODO switch case for handling meta == null (when testing)

    public static string GetMetaOrDefault<T>(this IDictionary<string, string> meta, string keyName, T @default = default)
        => meta.ContainsKey(keyName.ToLower()) ? meta[keyName.ToLower()] : @default?.ToString();

    public static bool HasMeta(this IEvent @event, string keyName)
        => @event.Meta.ContainsKey(keyName.ToLower());

    public static EventMetaData GetEventMetaData(this IDictionary<string, string> meta)
    => new EventMetaData
    (
        AggregateName: meta.GetMetaOrDefault<string>(nameof(EventMetaData.AggregateName)),
        CorrelationId: Guid.Parse(meta.GetMetaOrDefault<Guid>(nameof(EventMetaData.CorrelationId))),
        CausationId: Guid.Parse(meta.GetMetaOrDefault<string>(nameof(EventMetaData.CausationId))),
        EventId: Guid.Parse(meta.GetMetaOrDefault<Guid>(nameof(EventMetaData.EventId))),
        OccuredAt: long.Parse(meta.GetMetaOrDefault<long>(nameof(EventMetaData.OccuredAt))),
        StreamName: meta.GetMetaOrDefault<string>(nameof(EventMetaData.StreamName)),
        TriggeredBy: meta.GetMetaOrDefault<string>(nameof(EventMetaData.TriggeredBy)),
        BasedOnStreamVersion: long.Parse(meta.GetMetaOrDefault<long>(nameof(EventMetaData.BasedOnStreamVersion)))
    );

    public static void AddMetaData(this IDictionary<string, string> meta,
        long version,
        string streamName,
        string aggregateName,
        ICommand command,
        long occuredAt = default(long))
    => meta.AddMetaData(new EventMetaData
    (
        AggregateName: aggregateName,
        CorrelationId: command.CorrelationId,
        CausationId: command.CausationId,
        EventId: Guid.NewGuid(),
        OccuredAt: occuredAt == default(long) ? DateTime.UtcNow.Ticks : occuredAt,
        StreamName: streamName,
        TriggeredBy: command.GetType().Name,
        BasedOnStreamVersion: version
    ));

    public static void AddMetaData(this IDictionary<string, string> meta,
        string streamName,
        IEvent trigger,
        long occuredAt = default(long))
    => meta.AddMetaData(new EventMetaData
    (
        AggregateName: "unknown",
        CorrelationId: trigger.GetCorrelation(),
        CausationId: trigger.EventId(),
        EventId: Guid.NewGuid(),
        OccuredAt: occuredAt == default(long) ? DateTime.UtcNow.Ticks : occuredAt,
        StreamName: streamName,
        TriggeredBy: trigger.Event.GetType().Name,
        BasedOnStreamVersion: 0
    ));

    public static void AddMetaData(this IDictionary<string, string> meta, EventMetaData metaData)
    {
        if (metaData.AggregateName != default)
            meta[nameof(EventMetaData.AggregateName).ToLower()] = metaData.AggregateName;

        if (metaData.BasedOnStreamVersion != default)
            meta[nameof(EventMetaData.BasedOnStreamVersion).ToLower()] = metaData.BasedOnStreamVersion.ToString();

        meta[nameof(EventMetaData.StreamName).ToLower()] = metaData.StreamName;
        meta[nameof(EventMetaData.EventId).ToLower()] = metaData.EventId.ToString();
        meta[nameof(EventMetaData.CorrelationId).ToLower()] = metaData.CorrelationId.ToString();
        meta[nameof(EventMetaData.CausationId).ToLower()] = metaData.CausationId.ToString();
        meta[nameof(EventMetaData.TriggeredBy).ToLower()] = metaData.TriggeredBy;
        meta[nameof(EventMetaData.OccuredAt).ToLower()] = metaData.OccuredAt.ToString();
    }

    public static void AddStoreMetaData(this IDictionary<string, string> meta, EventStoreMetaData metaData)
    {
        meta[nameof(EventStoreMetaData.EventVersion).ToLower()] = metaData.EventVersion.ToString();
        meta[nameof(EventStoreMetaData.EventPosition).ToLower()] = metaData.EventPosition.ToString();
    }

    public static EventStoreMetaData GetEventStoreMetaData(this IDictionary<string, string> meta)
        => new EventStoreMetaData
        {
            EventPosition = long.Parse(meta.GetMetaOrDefault<long>(nameof(EventStoreMetaData.EventPosition))),
            EventVersion = long.Parse(meta.GetMetaOrDefault<long>(nameof(EventStoreMetaData.EventVersion))),
        };

    public static IEvent[] AddMetaData(this IEvent[] events, ICommand command, string aggregateName, string streamName, long version)
    {
        if (!events.All(x => x.SourceId == command.AggregateId.Id))
            throw new InvalidOperationException($"Event SourceId not set or not matching the triggering command - {command.GetType()}");

        events
            .Where(x => x.Meta == null)
            .ForEach(x => x.Meta = new Dictionary<string, string>());

        events
            .ForEach(x => x
                    .Tap(e => e.Meta.AddMetaData(version, streamName, aggregateName, command))
                    .Tap(e => e.Meta.AddTypeInfo(e))
                );
        return events;
    }

    public static void AddMetaData(this IEvent[] events, IEvent trigger, string streamName)
    {
        if (!events.All(x => !string.IsNullOrWhiteSpace(x.SourceId)))
            throw new InvalidOperationException($"Event SourceId not set.");// or not matching the triggering event - {trigger.EventId()}");

        events
            .Where(x => x.Meta == null)
            .ForEach(x => x.Meta = new Dictionary<string, string>());

        events
            .ForEach(x => x
                    .Tap(e => e.Meta.AddMetaData(streamName, trigger))
                    .Tap(e => e.Meta.AddTypeInfo(e))
                );
    }

}
