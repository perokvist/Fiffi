using System;
using System.Collections.Concurrent;
using System.Data;

namespace Fiffi.InMemory;

public class InMemoryEventStore : IAdvancedEventStore
{
    readonly ConcurrentDictionary<string, IEvent[]> innerStore = new();
    IDictionary<string, IEvent[]> store => innerStore;

    public Task<long> AppendToStreamAsync(string streamName, IEvent[] events)
        => AppendToStreamAsync(streamName, (default, false), events);

    public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events)
     => AppendToStreamAsync(streamName, (version, true), events);

    public Task<long> AppendToStreamAsync(string streamName, (long version, bool check) concurreny, IEvent[] events)
     => events.Any() ?
        Task.FromResult(innerStore.AddOrUpdate(
                streamName,
                key => AppendToStream(Array.Empty<IEvent>(), key, concurreny, events, () => store.Values.Count()),
                (key, value) => AppendToStream(value, key, concurreny, events, () => store.Values.Count()))
         .Last().Meta.GetEventStoreMetaData().EventVersion //TODO better impl
         ) :
        Task.FromResult((long)0);

    static IEvent[] AppendToStream(IEvent[] currentValue, string streamName, (long version, bool check) concurreny, IEvent[] events, Func<long> positionProvider)
    {
        var lastVersion = currentValue.Any() ? currentValue.Last().Meta.GetEventStoreMetaData().EventVersion : 0;

        if (concurreny.check && lastVersion != concurreny.version)
            throw new DBConcurrencyException($"wrong version - expected {concurreny.version} but was {lastVersion} - in stream {streamName}");

        //TODO check duplicates in passed events
        var duplicates = events.Where(x => currentValue.Any(e => e.EventId() == x.EventId()));
        if (duplicates.Any())
            throw new Exception($"Tried to append duplicates in stream - {streamName}. {string.Join(',', duplicates.Select(d => $"{d.GetEventName()} - {d.EventId()}"))}");


        var position = positionProvider(); //TODO naive

        var newStream = currentValue
            .Concat(events.Select((e, i) => e.Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = lastVersion + (i + 1), EventPosition = position + (i + 1) }))))
            .ToArray();

        return newStream;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            store.ContainsKey(streamName) ? (store[streamName].Where(x => x.Meta.GetEventStoreMetaData().EventVersion >= version).ToArray(), store[streamName].Last().Meta.GetEventStoreMetaData().EventVersion) : (Array.Empty<IEvent>(), 0);

    public async IAsyncEnumerable<IEvent> LoadEventStreamAsAsync(string streamName, long version)
    {
        var (events, _) = await LoadEventStreamAsync(streamName, version);
        foreach (var e in events)
            yield return e;
    }

    public IAsyncEnumerable<IEvent> LoadEventStreamAsAsync(string streamName, params IStreamFilter[] filters)
    {
        throw new NotImplementedException();
    }
}
