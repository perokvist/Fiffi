using CloudNative.CloudEvents;
using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.CloudEvents.Tests
{
    public class CloudEventStoreTests
    {
        [Fact]
        public async Task tAsync()
        {
            var store = new CloudEventStore(new InMemory.InMemoryEventStore<IDictionary<string, object>>(
                 x => (long)x["eventversion"],
                 (x, v) => x["eventversion"] = v,
                 x => (Guid)x["eventid"]
                ));

            var ce = new CloudEvent(CloudEventsSpecVersion.V1_0,
                "testevent",
                new Uri($"urn:test"),
                "test",
                id: Guid.NewGuid().ToString(),
                time: DateTime.UtcNow,
                new EventMetaDataExtension().Tap(x => x.MetaData =
                    new EventMetaData(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "test", "test", 0, "test", DateTime.UtcNow.Ticks)))
            {
                DataContentType = new ContentType(MediaTypeNames.Application.Json),
                Data = new TestEventRecord("hey")
            };

            _ = await store.AppendToStreamAsync("test", 0, ce);

            var r = await store.LoadEventStreamAsync("test", 0);
            var e = r.Events.First();
            var t = e.Data.GetType();

            Assert.Equal(1, e.Extension<EventStoreMetaDataExtension>().MetaData.EventVersion);
            Assert.Equal("test", e.Extension<EventMetaDataExtension>().MetaData.StreamName);
        }
    }
}
