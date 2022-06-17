using Fiffi.Serialization;
using Fiffi.Testing;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.FireStore.Tests;

public class EventStoreTests
{
    private FirestoreDb store;
    private readonly JsonSerializerOptions options;

    public EventStoreTests()
    {
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

        options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                 .Tap(x => x.AddConverters());

        var b = new FirestoreDbBuilder
        {
            EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
            ProjectId = "dummy-project",
            ConverterRegistry = new ConverterRegistry
                {
                    new EventDataConverter()
                }
        };
        store = b.Build();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task LoadEventFromBlankAsync()
    {
        var eventStore = new FireStoreEventStore(store);
        var streamName = $"test-stream-{Guid.NewGuid()}";
        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Empty(r.Events);
        Assert.Equal(0, r.Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteAsync()
    {
        var e = new EventData("test", "testEvent", new Dictionary<string, object> { { "testprop", "message" } });

        await store.Document("testing/per").SetAsync(new Dictionary<string, object> { { "version", "0" } });
        await store.Collection("testing/per/events").AddAsync(e.Data);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAsync()
    {
        var eventStore = new FireStoreEventStore(store);

        var streamName = $"test-stream-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(Guid.NewGuid().ToString(), "testEvent", new Dictionary<string, object> { { "eventprop", "test" } }));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadAsync()
    {
        var eventStore = new FireStoreEventStore(store);
        var streamName = $"test-stream-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(Guid.NewGuid().ToString(), "testEvent", new Dictionary<string, object> { { "eventprop", "test" } }));

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadEventAsync()
    {
        var eventStore = new FireStoreEventStore(store);
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var env = EventEnvelope.Create("sourceId", new TestEventRecord("testing"));
        var covert = Serialization.Extensions.AsMap();
        var map = covert(env, options);

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(Guid.NewGuid().ToString(), "testEvent", map));

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadEventsInOrderAsync()
    {
        var eventStore = new FireStoreEventStore(store);
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var env = EventEnvelope.Create("sourceId", new TestEventRecord("testing"));
        var covert = Serialization.Extensions.AsMap();
        var map = covert(env, options);

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(Guid.NewGuid().ToString(), "testEvent", map));

        await eventStore.AppendToStreamAsync(streamName, 1,
            new EventData(Guid.NewGuid().ToString(), "testEvent 2", map));

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(2, r.Version);
        Assert.Equal(1, r.Events.First().Version);
        Assert.Equal(2, r.Events.Last().Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadEventsAsAsync()
    {
        var eventStore = new FireStoreEventStore(store);
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var env = EventEnvelope.Create("sourceId", new TestEventRecord("testing"));
        var covert = Serialization.Extensions.AsMap();
        var map = covert(env, options);

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(Guid.NewGuid().ToString(), "testEvent", map));

        await eventStore.AppendToStreamAsync(streamName, 1,
            new EventData(Guid.NewGuid().ToString(), "testEvent 2", map));

        var r = await eventStore.LoadEventStreamAsAsync(streamName, 0).ToListAsync();

        Assert.Equal(2, r.Count());
        Assert.Equal(1, r.First().Version);
        Assert.Equal(2, r.Last().Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadEnvelopesAsync()
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord)));
        var eventStore = new EventStore(new FireStoreEventStore(store), options, resolver);
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var events = new[] { new TestEventRecord("testing") }
        .Cast<EventRecord>()
        .ToArray()
        .ToEnvelopes("test")
        .ForEach(e => e.AddTestMetaData(streamName))
        .ToArray();

        await eventStore.AppendToStreamAsync(streamName, 0, events);

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
        Assert.Equal("testing", ((TestEventRecord)r.Events.First().Event).Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadSubCollectionEnvelopesAsync()
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord)));
        var fireStoreEventStore = new FireStoreEventStore(store)
        {
            DocumentPathProvider = DocumentPathProviders.SubCollection()
        };
        var eventStore = new EventStore(fireStoreEventStore, options, resolver);

        var streamName = $"/clients/testclient/eventstore/test-{Guid.NewGuid()}";

        var events = new[] { new TestEventRecord("testing") }
        .Cast<EventRecord>()
        .ToArray()
        .ToEnvelopes("test")
        .ForEach(e => e.AddTestMetaData(streamName))
        .ToArray();

        await eventStore.AppendToStreamAsync(streamName, 0, events);

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
        Assert.Equal("testing", ((TestEventRecord)r.Events.First().Event).Message);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadEnvelopesComplexAsync()
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(ComplexEvent)));
        var eventStore = new EventStore(new FireStoreEventStore(store), options, resolver);
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var events = new[] {
            new ComplexEvent(Guid.NewGuid(), "testing",
            Location.Kitchen, true, DateTime.UtcNow, Array.Empty<Uri>()
            )
        }
        .Cast<EventRecord>()
        .ToArray()
        .ToEnvelopes("test")
        .ForEach(e => e.AddTestMetaData(streamName))
        .ToArray();

        await eventStore.AppendToStreamAsync(streamName, 0, events);

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
        Assert.Equal("testing", ((ComplexEvent)r.Events.First().Event).Description);
    }

}
public record ComplexEvent(Guid EventId = default, string Description = "",
    Location Location = Location.None, bool KeyApproved = false,
    DateTime Requested = default,
    Uri[] Images = default) : ComplexBase(EventId);

public record ComplexBase(Guid Id) : EventRecord;

public enum Location
{
    None,
    Kitchen
}

