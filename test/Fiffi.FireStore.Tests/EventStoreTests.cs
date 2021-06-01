using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.FireStore.Tests
{
    public class EventStoreTests
    {
        private FirestoreDb store;

        public EventStoreTests()
        {
            Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", "localhost:8080");

            var b = new FirestoreDbBuilder
            {
                EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly,
                ProjectId = "dummy-project"
            };
            store = b.Build();
        }

        [Fact]
        public async Task WriteAsync()
        {
            var e = new FireStoreEventStore.EventData("test", "testEvent", new Dictionary<string, object> { { "testprop", "message" } });

            await store.Document("testing/per").SetAsync(new Dictionary<string, object> { { "version", "0" } });
            await store.Collection("testing/per/events").AddAsync(e.Data);
        }


        [Fact]
        public async Task AppendAsync()
        {
            var eventStore = new FireStoreEventStore(store);

            await eventStore.AppendToStreamAsync("test-stream", 0,
                new FireStoreEventStore.EventData(Guid.NewGuid().ToString(), "testEvent", new Dictionary<string, object> { { "eventprop", "test" } }));
        }

        [Fact]
        public async Task AppendAndLoadAsync()
        {
            var eventStore = new FireStoreEventStore(store);
            var streamName = $"test-stream-{Guid.NewGuid()}";

            await eventStore.AppendToStreamAsync(streamName, 0,
                new FireStoreEventStore.EventData(Guid.NewGuid().ToString(), "testEvent", new Dictionary<string, object> { { "eventprop", "test" } }));

            var r = await eventStore.LoadEventStreamAsync(streamName, 0);

            Assert.Equal(1, r.Version);
        }
    }
}
