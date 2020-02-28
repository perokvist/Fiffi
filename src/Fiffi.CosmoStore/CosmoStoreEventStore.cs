using Microsoft.FSharp.Collections;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore
{
    public class CosmoStoreEventStore : IEventStore
    {
        private readonly global::CosmoStore.EventStore<JToken, long> store;
        private readonly Func<string, Type> typeResolver;

        public CosmoStoreEventStore(string connectionString,
            Func<string, Type> typeResolver)
        {
            this.store = global::CosmoStore.CosmosDb.EventStore
                .getEventStore(global::CosmoStore.CosmosDb
                .Configuration.CreateDefault(connectionString));
            this.typeResolver = typeResolver;
        }

        public async Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events)
        {
            var expectedNewVersion = version + 1;
            var position = version == 0 ? global::CosmoStore.ExpectedVersion<long>.NoStream : global::CosmoStore.ExpectedVersion<long>.NewExact(expectedNewVersion);

            await this.store.AppendEvents
                .Invoke(streamName)
                .Invoke(position)
                .Invoke(ListModule.OfSeq(events.Select(e => e.ToCosmosStoreEvent())));

            return expectedNewVersion;
        }

        public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var r = await this.store.GetEvents
                .Invoke(streamName)
                .Invoke(global::CosmoStore.EventsReadRange<long>.NewFromVersion(version));
            return (r.Select(x => x.ToEvent(this.typeResolver)), r.Any() ? r.Last().Version : 0); //TODO store meta
        }
    }
}
