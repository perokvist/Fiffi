﻿using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.FireStore
{
    public class FireStoreEventStore : IEventStore<EventData>
    {
        private readonly FirestoreDb store;

        public string StoreCollection { get; set; } = "eventstore";

        public FireStoreEventStore(FirestoreDb store)
        {
            this.store = store;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params EventData[] events)
            => store.RunTransactionAsync<long>(async tx =>
            {
                var headRef = store.Document($"{StoreCollection}/{streamName}");
                var head = await headRef.GetSnapshotAsync();
                long headVersion = 0;
                if (head.Exists)
                    headVersion = head.GetValue<long>("version");
                else
                    await headRef.CreateAsync(new Dictionary<string, object> { { "version", 0 } });

                if (headVersion != version)
                    throw new DBConcurrencyException($"stream head {streamName} have been updated. Expected {version}, streamversion {headVersion} ");

                if (!events.Any())
                    return headVersion;

                var versionedEvents = events
                                .Select((e, i) => new EventData(e.EventId, e.EventName, e.Data, headVersion + (i + 1)))
                                .ToArray();

                foreach (var item in versionedEvents)
                {
                    var eventsRef = headRef.Collection("events");
                    await eventsRef.Document($"{item.EventName}-{item.EventId}").CreateAsync(item);
                }

                var newVersion = versionedEvents.Last().Version;
                await headRef.SetAsync(new Dictionary<string, object> { { "version", newVersion } });
                return newVersion;
            });

        public async Task<(IEnumerable<EventData> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var headRef = store.Document($"{StoreCollection}/{streamName}");
            var head = await headRef.GetSnapshotAsync();
            if (!head.Exists)
                return (Enumerable.Empty<EventData>(), 0);

            var headVersion = head.GetValue<long>("version");

            if (headVersion == default)
                return (Enumerable.Empty<EventData>(), 0);
            if (headVersion < version)
                return (Enumerable.Empty<EventData>(), headVersion);

            var snapShot = await store
                .Collection($"{StoreCollection}/{streamName}/events")
                .WhereGreaterThanOrEqualTo(nameof(EventData.Version), version)
                .GetSnapshotAsync();

            var events = snapShot
                .Select(x => x.ConvertTo<EventData>())
                .OrderBy(x => x.Version)
                .ToArray();

            return (events, events.LastOrDefault()?.Version ?? headVersion); 
        }
    }
}
