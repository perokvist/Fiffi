using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Fiffi.Testing;

namespace Fiffi.Streamstone.Tests
{
    public class StreamoutboxTests
    {
        private CloudTable table;
        private IStateStore stateStore;
        private IStreamOutbox streamOutbox;

        public StreamoutboxTests()
        {
            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");

            table = storageAccount.CreateCloudTableClient().GetTableReference("fiffiintegration");
            table.DeleteIfExistsAsync().Wait();
            table.CreateIfNotExistsAsync().Wait();

            var tableBox = storageAccount.CreateCloudTableClient().GetTableReference("fiffiintegrationbox");
            tableBox.DeleteIfExistsAsync().Wait();
            tableBox.CreateIfNotExistsAsync().Wait();

            var es = new StreamStoneEventStore(table, TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent))));
            this.streamOutbox = new TableSteamOutbox(tableBox);

            this.stateStore = new NonTransactionalStateStore(es, this.streamOutbox);
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task PublishUsesOutBox()
        {
            //Given
            var id = new AggregateId(Guid.NewGuid());
            var e = new TestEvent()
            {
                SourceId = id.Id,
            }.AddTestMetaData<TestState>(id, -100);

            var events = new[] { e };

            await this.stateStore.SaveAsync<TestState>(id, null, 0, events);

            //When
            var pendingSet = false;
            await stateStore.OnPublish(async x =>
            {
                var p = await this.streamOutbox.GetPendingAsync(id.Id);
                pendingSet = p.Status == StreamPointerStatus.Pending;
            })(events);

            //Then
            Assert.True(pendingSet);
            Assert.True((await this.streamOutbox.GetPendingAsync(id.Id)) == null);
        }

        public class TestState
        {
        }

        public class TestEvent : IEvent
        {
            public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();

            public string SourceId { get; set; }
        }
    }
}
