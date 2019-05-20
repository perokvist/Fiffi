using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;
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

        public Task CancelAsync(string sourceId, params IEvent[] events) => CompleteAsync(sourceId, events);

        public async Task CompleteAsync(string sourceId, params IEvent[] events)
        {
            if (events.Any(x => x.SourceId != sourceId))
                throw new ArgumentException("Events not from same source");


            var deleteOperation = TableOperation.Delete(await GetEntityAsync(sourceId));
            await table.ExecuteAsync(deleteOperation);
        }

        public Task<StreamPointer[]> GetAllPendingAsync()
        {
            throw new NotImplementedException();
        }

        public Task<StreamPointer> GetPendingAsync(string sourceId) => GetAsync(sourceId);

        public async Task PendingAsync(IAggregateId id, string streamName, long version, params IEvent[] events)
        {
            var pointer = new StreamPointer(id.Id, streamName, version + 1);
            var insertOperation = TableOperation.Insert(new StreamPointerEntity(pointer));
            try
            {
                await table.ExecuteAsync(insertOperation);
            }
            catch (StorageException e) when (e.RequestInformation.HttpStatusCode == 409)
            {}
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
