using Fiffi.Testing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests
{
    public class ApplicationServiceTests
    {

        [Fact]
        public async Task ReadWriteAsync()
        {
            var id = Guid.NewGuid();
            var store = new InMemoryEventStore();
            var streamName = typeof(TestState).Name.AsStreamName(id).StreamName;
            var arrangeVersion = await store.AppendToStreamAsync(streamName, 0, new IEvent[] { new AggregateId(id).Pipe(x => new TestEvent(x).AddTestMetaData<string>(x)) });

            await ApplicationService.ExecuteAsync<TestState>
                (store, new TestCommand(id), 
                state => new IEvent[] { new TestEvent(id) }, e => Task.CompletedTask);

            var result = await store.LoadEventStreamAsync(streamName, 0);

            Assert.Equal(1, arrangeVersion);
            Assert.Equal(2, result.Version);
        }
    }
}
