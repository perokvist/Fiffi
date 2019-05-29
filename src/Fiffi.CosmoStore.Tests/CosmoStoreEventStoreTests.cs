using System;
using Xunit;
using Fiffi.Testing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.Documents.Client;

namespace Fiffi.CosmoStore.Tests
{
    public class CosmoStoreEventStoreTests
    {
        private Uri serviceUri = new Uri("https://localhost:8081");
        private const string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public CosmoStoreEventStoreTests()
        {
            using (var c = new DocumentClient(serviceUri, key))
            {
                var database = c.CreateDatabaseQuery()
                    .Where(x => x.Id == "EventStore")
                    .ToList();
                if (database.Any())
                {
                    c.DeleteDatabaseAsync(database.First().SelfLink)
                        .GetAwaiter()
                        .GetResult();
                }
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AppendAsync()
        {
            var s = new CosmoStoreEventStore(serviceUri, key,
                 TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent))));

            _ = await s.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("id")) });
            _ = await s.AppendToStreamAsync("test", 1, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("id")) });

            var r = await s.LoadEventStreamAsync("test", 0);
            var e = r.Events.ToList();

            Assert.Equal(2, e.Count);
        }
        public class TestEvent : IEvent
        {
            public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

            public string SourceId { get; set; }
        }
    }
}
