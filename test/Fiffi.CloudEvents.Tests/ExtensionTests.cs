using CloudNative.CloudEvents;
using Fiffi.Serialization;
using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Fiffi.CloudEvents.Tests
{
    public class ExtensionTests
    {
        ITestOutputHelper helper;

        public ExtensionTests(ITestOutputHelper helper)
        {
            this.helper = helper;
        }

        [Fact]
        public async Task WriteAndReadEventWithMetaExtensionAsync()
        {
            var e = new List<CloudEvent>();
            await new InMemory.InMemoryEventStore()
                .ExecuteAsync(
                    new TestCommand("test"),
                    "testStream",
                    () => new[] { new TestEventRecord("test") },
                    events =>
                    {
                        e.AddRange(events.Select(x => x.ToCloudEvent()));
                        return Task.CompletedTask;
                    });

            var eventJson = e.First().ToJson(); 
            
            helper.WriteLine(eventJson);

            var readEvent = eventJson.ToEvent();
            var readEventJson = readEvent.ToJson();

            helper.WriteLine(readEventJson);

            Assert.Equal(eventJson, readEventJson);
        }

        [Fact(Skip = "debug")]
        public async Task SerializeCompare()
        {
            var e = EventEnvelope.Create("test", new TestEventRecord("hey"));
            e.Meta.AddTypeInfo(e);
            e.Meta.AddMetaData(new EventMetaData(new(), new(), new(), "testStream", "a", 0, "test", 0));
            var eventJson = await new CloudEventContent(e.ToCloudEvent(), ContentMode.Structured, new JsonEventFormatter()).ReadAsStringAsync();
            var json = JsonSerializer.Serialize<object>(e.ToCloudEvent());
            Assert.Equal(eventJson, json);
        }

        [Fact(Skip = "debug")]
        public void ToMap()
        {
            var e = EventEnvelope.Create("test", new TestEventRecord("hey"));
            e.Meta.AddTypeInfo(e);
            e.Meta.AddMetaData(new EventMetaData(new(), new(), new(), "testStream", "a", 0, "test", 0));
            var ce = e.ToCloudEvent();
            var map = ce.ToJson().ToMap();
        }
    }
}
