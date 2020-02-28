using System;
using Xunit;
using Fiffi.Testing;
using System.Threading.Tasks;
using Fiffi.CosmoStore.Configuration;

namespace Fiffi.CosmoStore.Tests
{
    public class ChangeFeedUtil
    {
        private Uri serviceUri = new Uri("https://localhost:8081");
        private const string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";


        [Fact]//(Skip = "Used to append events for changefeed")]
        [Trait("Category", "Integration")]
        public async Task AppendToEventStoreAsync()
        {
            var settings = new ModuleOptions
            {
                Key = key,
                ServiceUri = serviceUri
            };

            var s = new CosmoStoreEventStore(settings.ConnectionString,
                 TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent))));

            var r = await s.LoadEventStreamAsync("test", 0);
            var id = new AggregateId("id");
            _ = await s.AppendToStreamAsync("test", r.Version, new IEvent[] { new TestEvent().AddTestMetaData<string>(id) });
        }

    }
}
