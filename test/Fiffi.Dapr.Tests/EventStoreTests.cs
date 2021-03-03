using Dapr.EventStore;
using Fiffi.Serialization;
using Fiffi.Testing;
using global::Dapr.Client;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Dapr.Tests
{
    public class EventStoreTests
    {
        private readonly DaprClient client;
        private readonly string streamName;
        private readonly Fiffi.Dapr.DaprEventStore store;

        public EventStoreTests()
        {
            //Environment.SetEnvironmentVariable("DAPR_GRPC_PORT", "50001");
            var inDapr = Environment.GetEnvironmentVariable("DAPR_GRPC_PORT") != null;
            global::Dapr.EventStore.DaprEventStore store = null;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            .Tap(x => x.Converters.Add(new EventRecordConverter()))
            .Tap(x => x.PropertyNameCaseInsensitive = true);

            if (inDapr)
            {
                client = new DaprClientBuilder()
                    .UseJsonSerializationOptions(options)
                    .Build();

                store = new global::Dapr.EventStore.DaprEventStore(client, NullLogger<global::Dapr.EventStore.DaprEventStore>.Instance)
                {
                    StoreName = "localcosmos",
                    MetaProvider = stream => new Dictionary<string, string>
                    {
                        { "partitionKey", streamName }
                    },
                };
            }
            else
            {
                client = new StateTestClient();
                store = new global::Dapr.EventStore.DaprEventStore(
                    new StateTestClient(),
                    NullLogger<global::Dapr.EventStore.DaprEventStore>.Instance);
            }

            streamName = $"teststream-{Guid.NewGuid().ToString().Substring(0, 5)}";

            this.store = new DaprEventStore(
                store,
                TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(GameCreated), typeof(TestEventRecord))),
                options,
                Serialization.Extensions.AsMap(),
                (ex, s, o) => { });
        }

        [Fact]
        public async Task LoadReturnsDeserializedEnvelope()
        {
            var @event = EventEnvelope.Create("test", new TestEventRecord("hellos"));
            @event.AddTestMetaData(streamName);
            _ = await store.AppendToStreamAsync(streamName, 0, @event);
            var stream = await store.LoadEventStreamAsync(streamName, 0);

            Assert.Equal("hellos", ((TestEventRecord)stream.Events.First().Event).Message);
        }

        [Fact]
        public async Task LoadReturnsDeserializedEnvelopeAsBaseClass()
        {
            var events = new[] { new GameCreated(Guid.NewGuid(), "tester", "test event", 5, DateTime.UtcNow) }
                .Cast<EventRecord>()
                .ToArray()
                .ToEnvelopes("test")
                .ForEach(e => e.AddTestMetaData(streamName))
                .ToArray();
            _ = await store.AppendToStreamAsync(streamName, 0, events);
            var stream = await store.LoadEventStreamAsync(streamName, 0);

            Assert.Equal("tester", ((GameCreated)stream.Events.First().Event).PlayerId);
        }

        [Fact]
        public void SerializeEnvelope()
        {
            var events = new[] { new GameCreated(Guid.NewGuid(), "tester", "test event", 5, DateTime.UtcNow) }
                .Cast<EventRecord>()
                .ToArray()
                .ToEnvelopes("test")
                .ForEach(e => e.AddTestMetaData(streamName))
                .ToArray();

            var opt = new JsonSerializerOptions().Tap(x => x.Converters.Add(new EventRecordConverter()));
            var json = JsonSerializer.Serialize(events.First(), opt);
            var element = JsonSerializer.Deserialize<object>(json);
            var data = EventData.Create("GameCreated", element, 1);
            var result = Fiffi.Dapr.DaprEventStore.ToEvent()(data, typeof(GameCreated), opt);

            Assert.Equal("tester", ((GameCreated)result.Event).PlayerId);
        }

        public enum GameStatus
        {
            None = 0,
            ReadyToStart = 10,
            Started = 20,
            Ended = 50
        }

        public record GameCreated(
                 Guid GameId,
                string PlayerId,
                string Title,
                int Rounds,
                DateTime Created,
                GameStatus Status = GameStatus.Started
            ) : EventRecord;
    }
}
