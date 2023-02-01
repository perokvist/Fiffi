using Fiffi.Testing;
using Google.Cloud.Firestore;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using static Fiffi.FireStore.DocumentPathProviders;
using PathProvider = System.Func<Google.Cloud.Firestore.FirestoreDb, Fiffi.FireStore.DocumentPathProviders.StreamContext, System.Threading.Tasks.Task<Fiffi.FireStore.DocumentPathProviders.StreamPaths>>;
namespace Fiffi.FireStore.Tests;

public class FireStoreEventStoreTests
{
    private FirestoreDb store;
    private readonly JsonSerializerOptions options;

    public FireStoreEventStoreTests()
    {
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

        options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                 .Tap(x => x.AddConverters());

        var b = new FirestoreDbBuilder
        {
            EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
            ProjectId = "demo-project",
            ConverterRegistry = new ConverterRegistry
                {
                    new EventDataConverter()
                }
        };
        store = b.Build();
    }


    public static PathProvider Test() =>
     async (store, ctx) =>
     {
         var modCtx = ctx with { Key = $"Clients/TestClient/eventstore/{ctx.Key}" };
         var r = await SubCollectionAll()(store, modCtx);
         return r;
     };

    public static IEnumerable<object[]> GetProviders()
        => new List<object[]>
        {
            new object[] { All() },
            new object[] { SubCollectionByPartition() },
            new object[] { Test() }

        };

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task LoadEventFromBlankAsync(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p };
        var streamName = $"test-stream-{Guid.NewGuid()}";
        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Empty(r.Events);
        Assert.Equal(0, r.Version);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteAsync()
    {
        var e = new EventData("test-stream", "test", "testEvent", new Dictionary<string, object> { { "testprop", "message" } }, DateTime.UtcNow);

        await store.Document("testing/per").SetAsync(new Dictionary<string, object> { { "version", "0" } });
        await store.Collection("testing/per/events").AddAsync(e.Data);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAsync(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p };

        var streamName = $"test-stream-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent", new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendWrappedAsync(PathProvider p)
    {
        var jsonSerializerOptions =
                            new JsonSerializerOptions().AddConverters()
                           .Tap(x => x.PropertyNameCaseInsensitive = true);

        var eventStore = new EventStore(new FireStoreEventStore(store) { DocumentPathProvider = p }, jsonSerializerOptions, s => typeof(string));

        var streamName = $"test-stream-{Guid.NewGuid()}";
        var env = EventEnvelope
            .Create("sourceId", new TestEventRecord("testing"))
            .AddTestMetaData(streamName);

        await eventStore.AppendToStreamAsync(streamName, 0, env);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAndLoadWrappedAsync(PathProvider p)
    {
        var jsonSerializerOptions =
                            new JsonSerializerOptions().AddConverters()
                           .Tap(x => x.PropertyNameCaseInsensitive = true);

        var eventStore = new EventStore(
            new FireStoreEventStore(store) { DocumentPathProvider = p },
            jsonSerializerOptions,
            TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord))));

        var streamName = $"test-stream-{Guid.NewGuid()}";
        var env = EventEnvelope
            .Create("sourceId", new TestEventRecord("testing"))
            .AddTestMetaData(streamName);

        await eventStore.AppendToStreamAsync(streamName, 0, env);

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
        Assert.IsType<TestEventRecord>(r.Events.First().Event);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAndLoadAsync(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p };

        var streamName = $"test-stream-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent", new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAndLoadEventAsync(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p };
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var env = EventEnvelope.Create("sourceId", new TestEventRecord("testing"));
        var covert = Serialization.Extensions.AsMap();
        var map = covert(env, options);

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent", map, DateTime.UtcNow));

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(1, r.Version);
    }

    [Fact(Skip = "Needs clean data")]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadAllEventAsync()
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = SubCollectionAll() };
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var env = EventEnvelope.Create("sourceId", new TestEventRecord("testing"));
        var covert = Serialization.Extensions.AsMap();
        var map = covert(env, options);

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent", map, DateTime.UtcNow));

        var r = await eventStore.LoadEventStreamAsync("$all", 0);

        Assert.Single(r.Events);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAndLoadEventsInOrderAsync(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p };
        var streamName = $"test-stream-{Guid.NewGuid()}";

        var env = EventEnvelope.Create("sourceId", new TestEventRecord("testing"));
        var covert = Serialization.Extensions.AsMap();
        var map = covert(env, options);

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent", map, DateTime.UtcNow));

        await eventStore.AppendToStreamAsync(streamName, 1,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent 2", map, DateTime.UtcNow));

        var r = await eventStore.LoadEventStreamAsync(streamName, 0);

        Assert.Equal(2, r.Version);
        Assert.Equal(1, r.Events.First().Version);
        Assert.Equal(2, r.Events.Last().Version);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAndLoadEventsAsAsync(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p };

        var streamName = $"test-stream-{Guid.NewGuid()}";

        var env = EventEnvelope.Create("sourceId", new TestEventRecord("testing"));
        var covert = Serialization.Extensions.AsMap();
        var map = covert(env, options);

        await eventStore.AppendToStreamAsync(streamName, 0,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent", map, DateTime.UtcNow));

        await eventStore.AppendToStreamAsync(streamName, 1,
            new EventData(streamName, Guid.NewGuid().ToString(), "testEvent 2", map, DateTime.UtcNow));

        var r = await eventStore.LoadEventStreamAsAsync(streamName, 0).ToListAsync();

        Assert.Equal(2, r.Count());
        Assert.Equal(1, r.First().Version);
        Assert.Equal(2, r.Last().Version);
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAndLoadEnvelopesAsync(PathProvider p)
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord)));
        var eventStore = new EventStore(new FireStoreEventStore(store) { DocumentPathProvider = p }, options, resolver);
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

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task AppendAndLoadEnvelopesComplexAsync(PathProvider p)
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(ComplexEvent)));
        var eventStore = new EventStore(new FireStoreEventStore(store) { DocumentPathProvider = p }, options, resolver);
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CategoryFilterAllProvider()
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = All() };

        var category = $"foo{Guid.NewGuid()}";
        var streamName = $"{category}-";
        var writeStream = $"some-stream-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(writeStream, 0,
            new EventData($"{streamName}88", Guid.NewGuid().ToString(), "testEvent",
                new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        await eventStore.AppendToStreamAsync(writeStream, 1,
         new EventData($"{streamName}99", Guid.NewGuid().ToString(), "testEvent",
             new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        await eventStore.AppendToStreamAsync(writeStream, 2,
         new EventData($"baz-01", Guid.NewGuid().ToString(), "testEvent",
             new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        var r = eventStore.LoadEventStreamAsAsync(
            "$all", new CategoryStreamFilter(category));

        Assert.Equal(2, await r.CountAsync());
        Assert.All(await r.ToArrayAsync(), x => Assert.StartsWith(category, x.EventStreamId));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CategoryFilterWithInStreamAllProvider()
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = All() };

        var streamName = $"foo-{Guid.NewGuid()}";
        var writeStream = $"some-stream-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(writeStream, 0,
            new EventData($"{streamName}88", Guid.NewGuid().ToString(), "testEvent",
                new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        await eventStore.AppendToStreamAsync(writeStream, 1,
         new EventData($"{streamName}99", Guid.NewGuid().ToString(), "testEvent",
             new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        await eventStore.AppendToStreamAsync(writeStream, 2,
         new EventData($"baz-01", Guid.NewGuid().ToString(), "testEvent",
             new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        var r = eventStore.LoadEventStreamAsAsync(
            $"{streamName}88", new CategoryStreamFilter("foo"));

        Assert.Equal(1, await r.CountAsync());
        Assert.All(await r.ToArrayAsync(), x => Assert.StartsWith("foo", x.EventStreamId));
    }


    [Fact]
    [Trait("Category", "Integration")]
    public async Task CategoryFilterSubProvider()
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord)));
        var eventStore = new AdvancedEventStore(new FireStoreEventStore(store) { DocumentPathProvider = SubCollectionByPartition() }, options, resolver);

        var streamName = $"category-stream-{Guid.NewGuid()}";

        var categoryEvents = Enumerable.Range(0, 2)
        .Select(x => new TestEventRecord("testing"))
        .Cast<EventRecord>()
        .ToArray()
        .ToEnvelopes("test")
        .ForEach(e => e.AddTestMetaData(streamName))
        .ToArray();

        var streamName2 = $"baz-stream-{Guid.NewGuid()}";

        var categoryEvents2 = Enumerable.Range(0, 1)
        .Select(x => new TestEventRecord("testing"))
        .Cast<EventRecord>()
        .ToArray()
        .ToEnvelopes("test")
        .ForEach(e => e.AddTestMetaData(streamName2))
        .ToArray();

        var eventsToAppend = categoryEvents.Concat(categoryEvents2).ToArray();
        await eventStore.AppendToStreamAsync("all2", 0, eventsToAppend);

        var r = eventStore.LoadEventStreamAsAsync("all2", new CategoryMetaDataStreamFilter("category"));

        Assert.Equal(2, await r.CountAsync());
        Assert.All(await r.ToArrayAsync(), x => Assert.StartsWith("category", x.GetStreamName()));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendEventWithMetaDataHasOwnStreamId()
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord)));
        var innerStore = new FireStoreEventStore(store) { DocumentPathProvider = SubCollectionByPartition() };
        var eventStore = new AdvancedEventStore(innerStore, options, resolver);

        var streamName = $"category-stream-{Guid.NewGuid()}";

        var events = Enumerable.Range(0, 2)
        .Select(x => new TestEventRecord("testing"))
        .Cast<EventRecord>()
        .ToArray()
        .ToEnvelopes("test")
        .ForEach(e => e.AddTestMetaData(streamName))
        .ToArray();

        await eventStore.AppendToStreamAsync("all", 0, events);

        var r = innerStore.LoadEventStreamAsAsync("all", 0);

        Assert.Equal(2, await r.CountAsync());
        Assert.All(await r.ToArrayAsync(), x => Assert.Equal("all", x.EventStreamId));
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task DateFilter(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p } as IAdvancedEventStore<EventData>;

        var stream = $"Test-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(stream, 0,
            new EventData(stream, Guid.NewGuid().ToString(), "testEvent",
                new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow.AddDays(-5)));

        await eventStore.AppendToStreamAsync(stream, 1,
         new EventData(stream, Guid.NewGuid().ToString(), "testEvent",
             new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        await eventStore.AppendToStreamAsync(stream, 2,
         new EventData(stream, Guid.NewGuid().ToString(), "testEvent",
             new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow));

        var r = eventStore.LoadEventStreamAsAsync(
            stream, new DateStreamFilter(DateTime.UtcNow.AddDays(-3), DateTime.UtcNow));

        Assert.Equal(2, await r.CountAsync());
    }

    [Theory]
    [Trait("Category", "Integration")]
    [MemberData(nameof(GetProviders))]
    public async Task DateFilterWithInStream(PathProvider p)
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = p } as IAdvancedEventStore<EventData>;

        var stream = $"Test-{Guid.NewGuid()}";
        var anotherStream = $"Test-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(stream, 0,
            new EventData(stream, Guid.NewGuid().ToString(), "testEvent",   
                new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow.AddDays(-20)));

        await eventStore.AppendToStreamAsync(anotherStream, 0,
            new EventData(anotherStream, Guid.NewGuid().ToString(), "testEvent2",
                new Dictionary<string, object> { { "eventprop", "test2" } }, DateTime.UtcNow.AddDays(-20)));

        var streamResult = eventStore.LoadEventStreamAsAsync(
            stream, new DateStreamFilter(DateTime.UtcNow.AddDays(-25), DateTime.UtcNow.AddDays(-15)));

        Assert.Equal(1, await streamResult.CountAsync());
    }

    [Fact(Skip = "clashes with other test data :O. Run on its own")]
    [Trait("Category", "Integration")]
    public async Task DateFilterWithInStreamAll()
    {
        var eventStore = new FireStoreEventStore(store) { DocumentPathProvider = Test() } as IAdvancedEventStore<EventData>;

        var stream = $"Test-{Guid.NewGuid()}";
        var anotherStream = $"Test-{Guid.NewGuid()}";

        await eventStore.AppendToStreamAsync(stream, 0,
            new EventData(stream, Guid.NewGuid().ToString(), "testEvent",
                new Dictionary<string, object> { { "eventprop", "test" } }, DateTime.UtcNow.AddDays(-40)));

        await eventStore.AppendToStreamAsync(anotherStream, 0,
            new EventData(anotherStream, Guid.NewGuid().ToString(), "testEvent2",
                new Dictionary<string, object> { { "eventprop", "test2" } }, DateTime.UtcNow.AddDays(-40)));

        var allResult = eventStore.LoadEventStreamAsAsync(
            "$all", new DateStreamFilter(DateTime.UtcNow.AddDays(-45), DateTime.UtcNow.AddDays(-30)));

        Assert.Equal(2, await allResult.CountAsync());
    }


    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppendAndLoadSubCollectionEnvelopesAsync()
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord)));
        var fireStoreEventStore = new FireStoreEventStore(store)
        {
            DocumentPathProvider = DocumentPathProviders.SubCollectionByPartition()
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

    [Fact(Skip = "clashes with other test data :O. Run on its own")]
    [Trait("Category", "Integration")]
    public async Task AllStreamIsolationAsync()
    {
        var resolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEventRecord)));
        var fireStoreEventStore = new FireStoreEventStore(store)
        {
            DocumentPathProvider = DocumentPathProviders.SubCollectionAll()
        };
        var eventStore = new EventStore(fireStoreEventStore, options, resolver);

        var streamSufix = $"test -{Guid.NewGuid()}";
        var streamName = $"/clients/testclient/eventstore/{streamSufix}";

        var events = new[] { new TestEventRecord("testing") }
        .Cast<EventRecord>()
        .ToArray()
        .ToEnvelopes("test")
        .ForEach(e => e.AddTestMetaData(streamName))
        .ToArray();

        _ = await eventStore.AppendToStreamAsync(streamName, 0, events);

        var allStream = "all";

        var events2 = new[] { new TestEventRecord("testing2") }
            .Cast<EventRecord>()
            .ToArray()
            .ToEnvelopes("test2")
            .ForEach(e => e.AddTestMetaData(allStream))
            .ToArray();

        _ = await eventStore.AppendToStreamAsync(allStream, 0, events);

        var s1 = await eventStore.LoadEventStreamAsync(streamName.Replace(streamSufix, "$all"), 0);
        var s2 = await eventStore.LoadEventStreamAsync(allStream, 0);

        Assert.Single(s1.Events);
        Assert.Single(s2.Events);


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

}

