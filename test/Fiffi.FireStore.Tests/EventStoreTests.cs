using Fiffi.Serialization;
using Fiffi.Testing;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.FireStore.Tests
{
    public class EventStoreTests
    {
        private FirestoreDb store;
        private readonly JsonSerializerOptions options;

        public EventStoreTests()
        {
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

            options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                .Tap(x => x.Converters.Add(new DictionaryStringObjectJsonConverter()))
                .Tap(x => x.Converters.Add(new EventRecordConverter()))
                .Tap(x => x.PropertyNameCaseInsensitive = true);

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
    }
}
