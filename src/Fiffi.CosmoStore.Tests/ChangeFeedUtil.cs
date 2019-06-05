using System;
using Xunit;
using Fiffi.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.Documents.Client;

namespace Fiffi.CosmoStore.Tests
{
    public class ChangeFeedUtil
    {
        private Uri serviceUri = new Uri("https://localhost:8081");
        private const string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";


        [Fact(Skip = "Used to append events for changefeed")]
        [Trait("Category", "Integration")]
        public async Task AppendToEventStoreAsync()
        {
            var s = new CosmoStoreEventStore(serviceUri, key,
                 TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent))));

            var r = await s.LoadEventStreamAsync("test", 0);

            _ = await s.AppendToStreamAsync("test", r.Version, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("id")) });
        }

        public class TestEvent : IEvent
        {
            public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

            public string SourceId { get; set; }
        }
    }
}
