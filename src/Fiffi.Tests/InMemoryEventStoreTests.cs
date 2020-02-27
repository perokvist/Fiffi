using Fiffi.Testing;
using System.Data;
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
        public async Task ConcurrencyAsync()
        {
            var store = new InMemoryEventStore();

            await Assert.ThrowsAsync<DBConcurrencyException>(async () =>
            {
                await store.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t")) });
                await store.AppendToStreamAsync("test", 0, new IEvent[] { new TestEvent().AddTestMetaData<string>(new AggregateId("t2")) });
            });
        }
    }
}
