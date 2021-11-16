using CloudNative.CloudEvents;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi.CloudEvents;

public class CloudEventStore : IEventStore<CloudEvent>
{
    private readonly IEventStore<EventData> eventStore;
    private readonly JsonSerializerOptions jsonSerializerOptions;

    public CloudEventStore(
        IEventStore<EventData> eventStore,
        JsonSerializerOptions jsonSerializerOptions
        )
    {
        this.eventStore = eventStore;
        this.jsonSerializerOptions = jsonSerializerOptions;
    }

    public Task<long> AppendToStreamAsync(string streamName, long version, params CloudEvent[] events)
     => eventStore.AppendToStreamAsync(streamName, version, events.Select(e => e.ToEventData(e.Extension<EventMetaDataExtension>().MetaData.EventId.ToString())).ToArray());

    public async Task<(IEnumerable<CloudEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
    {
        var (events, v) = await eventStore.LoadEventStreamAsync(streamName, version);
        var ce = events.Select(e => e.ToEvent(jsonSerializerOptions));
        return (ce, v);
        //.Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version }))),
        //v);
    }
}
