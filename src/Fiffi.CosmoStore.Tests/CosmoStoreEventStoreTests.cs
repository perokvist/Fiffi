using System;
using Xunit;
using System.Threading.Tasks;
using System.Linq;
using Fiffi.Testing;

namespace Fiffi.CosmoStore.Tests
{
    public class CosmoStoreEventStoreTests
    {
        private Uri serviceUri = new Uri("https://localhost:8081/");
        private const string key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public CosmoStoreEventStoreTests()
        {
            //Database.DeleteEventStoreAsync(serviceUri, key)
            //    .GetAwaiter()
            //    .GetResult();
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task AppendAsync()
        {
            var settings = new ModuleOptions
            {
                ServiceUri = serviceUri,
                Key = key
            };

            var s = new CosmoStoreEventStore(settings.ConnectionString,
                 TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent))));

            _ = await s.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent("id").AddTestMetaData<string>(new AggregateId("id")) });
            _ = await s.AppendToStreamAsync("test", 1, new IEvent[] { new TestEvent("id").AddTestMetaData<string>(new AggregateId("id")) });

            var r = await s.LoadEventStreamAsync("test", 0);
            var e = r.Events.ToList();

            Assert.Equal(2, e.Count);
            Assert.True(e.All(x => x.Meta.Keys.Any()));
        }


        [Fact]
        [Trait("Category", "Integration")]
        public async Task AppendFiffiTestEventAsync()
        {
            var settings = new ModuleOptions
            {
                ServiceUri = serviceUri,
                Key = key
            };

            var s = new CosmoStoreEventStore(settings.ConnectionString,
                 TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(Fiffi.Testing.TestEvent))));
            var id = new AggregateId("id");
            _ = await s.AppendToStreamAsync("test", 0, new IEvent[] { new Fiffi.Testing.TestEvent(id).AddTestMetaData<string>(id) });
            _ = await s.AppendToStreamAsync("test", 1, new IEvent[] { new Fiffi.Testing.TestEvent(id).AddTestMetaData<string>(id) });

            var r = await s.LoadEventStreamAsync("test", 0);
            var e = r.Events.ToList();

            Assert.Equal(2, e.Count);
            Assert.Equal(id.Id, e.First().SourceId);
            Assert.True(e.All(x => x.Meta.Keys.Any()));
        }
    }
}
