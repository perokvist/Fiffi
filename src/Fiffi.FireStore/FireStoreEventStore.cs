using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Fiffi.FireStore.FireStoreEventStore;

namespace Fiffi.FireStore
{
    public class FireStoreEventStore : IEventStore<EventData>
    {
        private readonly FirestoreDb store;
        //private readonly JsonSerializerOptions jsonSerializerOptions;
        //private readonly Func<IEvent, JsonSerializerOptions, object> converter;
        //private readonly Func<string, Type> typeResolver;

        const string eventCollection = "eventstore";

        public FireStoreEventStore(FirestoreDb store)
        {
            this.store = store;
        }

        public Task<long> AppendToStreamAsync(string streamName, long version, params EventData[] events)
            => store.RunTransactionAsync<long>(async tx =>
            {
                var headRef = store.Document($"{eventCollection}/{streamName}");
                var head = await headRef.GetSnapshotAsync();
                long headVersion = 0;
                if (head.Exists)
                    headVersion = head.GetValue<long>("version");
                else
                    await headRef.CreateAsync(new Dictionary<string, object> { { "version", 0 } });

                if (!events.Any())
                    return headVersion;

                var versionedEvents = events
                                .Select((e, i) => new EventData(e.EventId, e.EventName, e.Data, headVersion + (i + 1)))
                                .ToArray();

                foreach (var item in versionedEvents)
                {
                    var eventsRef = headRef.Collection("events");
                    await eventsRef.Document($"{item.EventName}-{item.EventId}").CreateAsync(item.Data);
                }

                var newVersion = versionedEvents.Last().Version;
                await headRef.SetAsync(new Dictionary<string, object> { { "version", newVersion } });
                return newVersion;
            });

        public async Task<(IEnumerable<EventData> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
        {
            var headRef = store.Document($"{eventCollection}/{streamName}");
            var head = await headRef.GetSnapshotAsync();
            var headVersion = head.GetValue<long>("version");

            if (headVersion == default)
                return (Enumerable.Empty<EventData>(), 0);
            if(headVersion > version)
                return (Enumerable.Empty<EventData>(), headVersion);

            var snapShot = await store
                .Collection($"{eventCollection}/{streamName}/events")
                .WhereGreaterThan(nameof(EventData.Version), version)
                .GetSnapshotAsync();

            var events = snapShot
                .Select(x => x.ConvertTo<EventData>())
                .OrderBy(x => x.Version)
                .ToArray();

            //    var result = (events
            //.Select(e => toEvent(e, typeResolver(e.EventName), this.jsonSerializerOptions)
            //.Tap(x => x.Meta.AddStoreMetaData(new EventStoreMetaData { EventVersion = e.Version, EventPosition = e.Version })))
            //.AsEnumerable(),
            //events.LastOrDefault()?.Version ?? (meta?.Version ?? 0));


            return (events, events.LastOrDefault()?.Version ?? 0); //Todo head
        }

        //public static EventData ToEventData(IEvent e, Func<IEvent, object> f)
        //    => new(e.EventId().ToString(), e.Event.GetType().Name, f(e));

        public record EventData(string EventId, string EventName, object Data, long Version = 0)
        {
            public static EventData Create(string EventName, object Data, long Version = 0) => new(Guid.NewGuid().ToString(), EventName, Data, Version);
        }
    }
}
