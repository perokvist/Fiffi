using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.InMemory
{
    public class InMemoryEventStore : InMemoryEventStore<IEvent>, IAdvancedEventStore
    {
        public InMemoryEventStore() : base(
            x => x.Meta.GetEventStoreMetaData().EventVersion, 
            (x, v) => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = v, EventPosition = v }),
            x => x.EventId())
        {}
    }

    public class InMemoryEventStore<T> : IEventStore<T> 
    {
        readonly ConcurrentDictionary<string, T[]> innerStore = new ConcurrentDictionary<string, T[]>();
        private readonly Func<T, long> getVersion;
        private readonly Action<T, long> setVersion;
        private readonly Func<T, Guid> getEventId;

        IDictionary<string, T[]> store => innerStore;

        public InMemoryEventStore(
            Func<T, long> version,
            Action<T, long> setVersion,
            Func<T, Guid> getEventId)
        {
            this.getVersion = version;
            this.setVersion = setVersion;
            this.getEventId = getEventId;
        }

        public Task<long> AppendToStreamAsync(string streamName, T[] events)
            => AppendToStreamAsync(streamName, (default, false), events);

        public Task<long> AppendToStreamAsync(string streamName, long version, T[] events)
         => AppendToStreamAsync(streamName, (version, true), events);

        public Task<long> AppendToStreamAsync(string streamName, (long version, bool check) concurreny, T[] events)
         => events.Any() ?
            Task.FromResult(innerStore.AddOrUpdate(
                    streamName,
                    key => AppendToStream(Array.Empty<T>(), key, concurreny, events, () => store.Values.Count(), this.getVersion, this.setVersion, this.getEventId),
                    (key, value) => AppendToStream(value, key, concurreny, events, () => store.Values.Count(), this.getVersion, this.setVersion, this.getEventId))
             .Last().Pipe(x => getVersion(x)) //TODO better impl
             ) :
            Task.FromResult((long)0);

        static T[] AppendToStream(T[] currentValue, string streamName, (long version, bool check) concurreny, T[] events, Func<long> positionProvider, Func<T, long> getVersion, Action<T, long> setVersion, Func<T, Guid> getId)
        {
            var lastVersion = currentValue.Any() ? currentValue.Last().Pipe(x => getVersion(x)) : 0;

            if (concurreny.check && lastVersion != concurreny.version)
                throw new DBConcurrencyException($"wrong version - expected {concurreny.version} but was {lastVersion} - in stream {streamName}");

            //TODO check duplicates in passed events
            var duplicates = events.Where(x => currentValue.Any(e => getId(e) == getId(x)));
            if (duplicates.Any())
                throw new Exception($"Tried to append duplicates in stream - {streamName}. {string.Join(',', duplicates.Select(d => $"{getId(d)}"))}");

            var position = positionProvider(); //TODO naive

            var newStream = currentValue
                .Concat(events.Select((e, i) => e.Tap(x => setVersion(x, (i + 1)))))
                //.Concat(events.Select((e, i) => e.Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = lastVersion + (i + 1), EventPosition = position + (i + 1) }))))
                .ToArray();

            return newStream;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<(IEnumerable<T> Events, long Version)> LoadEventStreamAsync(string streamName, long version) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            store.ContainsKey(streamName) ? (store[streamName].Where(x => getVersion(x) >= version).ToArray(), store[streamName].Last().Pipe(x => getVersion(x))) : (new T[] { }, 0);

    }
}
