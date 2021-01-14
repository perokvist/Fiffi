using CloudNative.CloudEvents;
using Fiffi.Testing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

            var eventJson = await new CloudEventContent(e.First(), ContentMode.Structured, new JsonEventFormatter()).ReadAsStringAsync();
            
            helper.WriteLine(eventJson);

            var stream = new MemoryStream(Encoding.ASCII.GetBytes(eventJson));

            var readEvent = await new JsonEventFormatter().DecodeStructuredEventAsync(stream, Enumerable.Empty<ICloudEventExtension>());
            var readEventJson = await new CloudEventContent(readEvent, ContentMode.Structured, new JsonEventFormatter()).ReadAsStringAsync();

            Assert.Equal(eventJson, readEventJson);
        }
    }
}
