using Fiffi.InMemory;
using Fiffi.Testing;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class InMemoryEventStoreTests
    {

        [Fact]
        public async Task WriteAndReadAsync()
        {
            var store = new InMemoryEventStore();

            var v = await store.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t")) });
            var r = await store.LoadEventStreamAsync("test", 0);
            var v2 = await store.AppendToStreamAsync("test", r.Item2, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t"), (int)(r.Item2 + 1)) });
            var r2 = await store.LoadEventStreamAsync("test", 0);

            Assert.Equal(1, r.Item2);
            Assert.Equal(v, r.Item2);
            Assert.Equal(2, r2.Item2);
            Assert.Equal(v2, r2.Item2);
        }

        [Fact]
        public async Task VersionAndPositionAsync()
        {
            var store = new InMemoryEventStore();

            var version1 = await store.AppendToStreamAsync("test", 0,
                new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t")) });

            var version2 = await store.AppendToStreamAsync("test2", 0,
                new IEvent[] {
                    new TestEvent().AddTestMetaData<string>(new AggregateId("t2")),
                    new TestEvent().AddTestMetaData<string>(new AggregateId("t2"))
                });

            var stream1 = await store.LoadEventStreamAsync("test", 0);
            var stream2 = await store.LoadEventStreamAsync("test2", 0);

            Assert.Equal(1, version1);
            Assert.Equal(2, version2);
            Assert.Equal(1, stream1.Version);
            Assert.Equal(2, stream2.Version);
            Assert.Equal(1, stream1.Events.First().Meta.GetEventStoreMetaData().EventPosition);
            Assert.Equal(2, stream2.Events.First().Meta.GetEventStoreMetaData().EventPosition);
            Assert.Equal(3, stream2.Events.Last().Meta.GetEventStoreMetaData().EventPosition);
        }


        [Fact]
        public async Task ConcurrencyAsync()
        {
            var store = new InMemoryEventStore();

            await Assert.ThrowsAsync<DBConcurrencyException>(async () =>
            {
                await store.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t")) });
                await store.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t2")) });
            });
        }

        [Fact]
        public async Task LoadGetsVersionAsync()
        {
            var store = new InMemoryEventStore();
            _ = await Projections.ProjectionExtensions.AppendToStreamAsync(store, "test", new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t")) });
            var r = await store.LoadEventStreamAsync("test", 0);

            Assert.Equal(1, r.Version);
        }

        [Fact]
        public async Task LoadGetStartVersionAsync()
        {
            var store = new InMemoryEventStore();
            var r = await store.LoadEventStreamAsync("test", 0);

            Assert.Equal(0, r.Version);
        }

        [Fact]
        public async Task AppendWithoutVersionAsync()
        { 
            var store = new InMemoryEventStore();
            _ = await Projections.ProjectionExtensions.AppendToStreamAsync(store, "test", new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t")) });
            var r = await Projections.ProjectionExtensions.AppendToStreamAsync(store, "test", new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t")) });

            Assert.Equal(2, r);
        }
    }
}
