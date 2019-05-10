using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.Streamstone
{
    public class TableSteamOutbox : IStreamOutbox
    {
        private readonly CloudTable table;

        public TableSteamOutbox(CloudTable table)
        {
            this.table = table;
        }

        public Task CancelAsync(string sourceId) => CompleteAsync(sourceId);

        public async Task CompleteAsync(string sourceId)
        {
            var deleteOperation = TableOperation.Delete(await GetEntityAsync(sourceId));
            await table.ExecuteAsync(deleteOperation);
        }

        public Task<StreamPointer[]> GetAllPendingAsync()
        {
            throw new NotImplementedException();
        }

        public Task<StreamPointer> GetPendingAsync(string sourceId) => GetAsync(sourceId);

        public async Task PendingAsync(IAggregateId id, string streamName, long expectedNewVersion)
        {
            var insertOperation = TableOperation.Insert(new StreamPointerEntity(new StreamPointer(id.Id, streamName, expectedNewVersion)));
            await table.ExecuteAsync(insertOperation);
        }

        async Task<StreamPointerEntity> GetEntityAsync(string sourceId)
        {
            var retrieveOperation = TableOperation.Retrieve<StreamPointerEntity>("streamoutbox", sourceId);
            var r = await table.ExecuteAsync(retrieveOperation);
            var e = r.Result as StreamPointerEntity;
            return e;
        }


        async Task<StreamPointer> GetAsync(string sourceId)
        {
            StreamPointerEntity e = await GetEntityAsync(sourceId);
            if (e == null) return null;

            return new StreamPointer(e.RowKey, e.StreamName, e.Version);
        }
    }

    public class StreamPointerEntity : TableEntity
    {
        public StreamPointerEntity()
        {
        }

        public StreamPointerEntity(StreamPointer streamPointer) : base("streamoutbox", streamPointer.SourceId)
        {
            this.StreamName = streamPointer.StreamName;
            this.Version = streamPointer.Version;
        }

        public string StreamName { get; set; }

        public long Version { get; set; }
    }
}
