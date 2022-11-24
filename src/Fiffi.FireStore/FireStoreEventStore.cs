using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using static Fiffi.FireStore.DocumentPathProviders;

namespace Fiffi.FireStore;

public class FireStoreEventStore : IAdvancedEventStore<EventData>
{
    private readonly FirestoreDb store;

    public string StoreCollection { get; set; } = "eventstore";

    public Func<FirestoreDb, StreamContext, Task<StreamPaths>> DocumentPathProvider = All();
    public FireStoreEventStore(FirestoreDb store)
    {
        this.store = store;
    }

    public Task<long> AppendToStreamAsync(string streamName, long version,
        bool checkConcurreny = true, params EventData[] events)
        => store.RunTransactionAsync<long>(async tx =>
        {
            var ctx = await DocumentPathProvider(store, new(StoreCollection, streamName, true));

            var headRef = store.Document(ctx.HeaderPath);
            var head = await headRef.GetSnapshotAsync();
            long headVersion = 0;
            if (head.Exists)
                headVersion = head.GetValue<long>("version");
            else
                await headRef.CreateAsync(new Dictionary<string, object> {
                    { "version", 0 },
                });

            if (checkConcurreny && headVersion != version)
                throw new DBConcurrencyException($"stream head {streamName} have been updated. Expected {version}, streamversion {headVersion} ");

            if (!events.Any())
                return headVersion;

            var versionedEvents = events
                            .Select((e, i) => new EventData(e.EventStreamId, e.EventId, e.EventName, e.Data, e.Created, headVersion + (i + 1)))
                            .ToArray();

            foreach (var item in versionedEvents)
            {
                var eventsRef = store.Collection(ctx.StreamPath);
                await eventsRef.Document($"{item.EventName}-{item.EventId}").CreateAsync(item);
            }

            var newVersion = versionedEvents.Last().Version;
            await headRef.SetAsync(new Dictionary<string, object> { { "version", newVersion } });
            return newVersion;
        });

    public async Task<(IEnumerable<EventData> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
    {
        var ctx = await DocumentPathProvider(store, new(StoreCollection, streamName, true));

        var headRef = store.Document(ctx.HeaderPath);
        var head = await headRef.GetSnapshotAsync();
        if (!head.Exists)
            return (Enumerable.Empty<EventData>(), 0);

        var headVersion = head.GetValue<long>("version");

        if (headVersion == default)
            return (Enumerable.Empty<EventData>(), 0);
        if (headVersion < version)
            return (Enumerable.Empty<EventData>(), headVersion);

        var e = LoadEventStreamAsAsync(streamName, version);
        return (e.ToEnumerable(), headVersion);
    }

    public Task<long> AppendToStreamAsync(string streamName, params EventData[] events)
        => AppendToStreamAsync(streamName, default, false, events);

    public async IAsyncEnumerable<EventData> LoadEventStreamAsAsync(string streamName, long version)
    {
        var ctx = await DocumentPathProvider(store, new(StoreCollection, streamName, true));
        var headRef = store.Document(ctx.HeaderPath);
        var head = await headRef.GetSnapshotAsync();
        if (!head.Exists)
            yield break;

        var headVersion = head.GetValue<long>("version");

        if (headVersion == default)
            yield break;
        if (headVersion < version)
            yield break;

        var events = store
            .Collection(ctx.StreamPath)
            .WhereEqualTo(nameof(EventData.EventStreamId), streamName)
            .WhereGreaterThanOrEqualTo(nameof(EventData.Version), version)
            .OrderBy(nameof(EventData.Version))
            .StreamAsync()
            .Select(x => x.ConvertTo<EventData>());

        await foreach (var item in events)
            yield return item;
    }

    public Task<long> AppendToStreamAsync(string streamName, long version, params EventData[] events)
        => AppendToStreamAsync(streamName, version, true, events);

    public async IAsyncEnumerable<EventData> LoadEventStreamAsAsync(string streamName, params IStreamFilter[] filters)
    {
        var ctx = await DocumentPathProvider(store, new(StoreCollection, streamName, true));

        var eventStoreDoc = await store
         .Collection(ctx.StreamPath)
         .Pipe(x => streamName.EndsWith("$all") switch
             {
                 true => x,
                 _ => x.WhereEqualTo(nameof(EventData.EventStreamId), streamName)
             })
         .ApplyFilters(filters)
         .OrderBy(nameof(EventData.Created))
         .GetSnapshotAsync();
        var filtered = eventStoreDoc
            .Documents
            .ApplyFilters(filters)
            .Select(x => x.ConvertTo<EventData>())
            .ToAsyncEnumerable();
        await foreach (var item in filtered)
            yield return item;
    }
}
