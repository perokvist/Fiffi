﻿using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace Fiffi.FireStore;
public class EventDataConverter : IFirestoreConverter<EventData>
{
    public EventData FromFirestore(object value)
        => value switch
        {
            IDictionary<string, object> d => new(
                d.ContainsKey(nameof(EventData.EventStreamId)) ? d[nameof(EventData.EventStreamId)] as string : "unknown",
                d[nameof(EventData.EventId)] as string,
                d[nameof(EventData.EventName)] as string,
                d[nameof(EventData.Data)],
                d.ContainsKey(nameof(EventData.Created)) ? ((Timestamp)d[nameof(EventData.Created)]).ToDateTime() : DateTime.MinValue,
                Convert.ToInt64(d[nameof(EventData.Version)])),
            _ => throw new Exception($"object of unexpected type {value.GetType()}")
        };
    public object ToFirestore(EventData value)
        => new Dictionary<string, object>()
            .Tap(x => x.Add(nameof(value.EventStreamId), value.EventStreamId))
            .Tap(x => x.Add(nameof(value.EventId), value.EventId))
            .Tap(x => x.Add(nameof(value.EventName), value.EventName))
            .Tap(x => x.Add(nameof(value.Data), value.Data))
            .Tap(x => x.Add(nameof(value.Created), value.Created))
            .Tap(x => x.Add(nameof(value.Version), value.Version));
}
