using System;
using Xunit;
using Fiffi.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore.Tests
{
    public class CosmoStoreEventStoreTests
    {
        [Fact]
        [Trait("Category", "Integration")]
        public async Task AppendAsync()
        {
            var s = new CosmoStoreEventStore(new Uri("https://localhost:8081"),
                "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

            _ = await s.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("id")) });
            _ = await s.AppendToStreamAsync("test", 2, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("id")) });


        }

        public class TestEvent : IEvent
        {
            public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

            public string SourceId { get; set; }
        }
    }
}
