using Microsoft.FSharp.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore
{
    public class CosmoStoreEventStore : IEventStore
    {
        private readonly global::CosmoStore.EventStore store;

        public CosmoStoreEventStore(Uri serviceEndpoint, string authKey)
        {
            this.store = global::CosmoStore.CosmosDb.EventStore
                .getEventStore(global::CosmoStore.CosmosDb
                .Configuration.CreateDefault(serviceEndpoint, authKey));
        }

        public async Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events)
        {
            var expectedNewVersion = version + 1;
            var position = version == 0 ? global::CosmoStore.ExpectedPosition.NoStream : global::CosmoStore.ExpectedPosition.NewExact(expectedNewVersion);

            await this.store.AppendEvents
                .Invoke(streamName)
                .Invoke(position)
                .Invoke(ListModule.OfSeq(events.Select(e => e.ToCosmosStoreEvent())));

            return expectedNewVersion;
        }

        public Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            throw new NotImplementedException();
        }
    }
}
