using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fiffi;

namespace Fiffi.Projections
{
    public static class ProjectionExtensions
    {
        public static Projector<T> Projector<T>(this IEventStore store)
          where T : class, new()
          => new Projector<T>(store);


        public static async Task<long> AppendToStreamAsync(this IEventStore store, string streamName, params IEvent[] events)
        {
            var r = await store.LoadEventStreamAsync(streamName, 0); //TODO optimize :)
            return await store.AppendToStreamAsync(streamName, r.Version, events);
        }

        public static Task<T> GetAsync<T, TStream>(this IEventStore store, IAggregateId id)
            where T : class, new()
         => store.GetAsync<T>(typeof(TStream).Name.AsStreamName(id).StreamName);

        public static async Task<T> GetAsync<T>(this IEventStore store, string streamName)
            where T : class, new()
        {
            var r = await store.LoadEventStreamAsync(streamName, 0);
            var projection = r.Item1.Select(e => e.Event).Rehydrate<T>();
            return projection;
        }

        public static async Task<T> GetAsync<T>(this IEventStore store, string streamName, T current, Func<T, EventRecord, T> apply)
            where T : class
        {
            var r = await store.LoadEventStreamAsync(streamName, 0);
            var projection = r.Item1.Select(e => e.Event).Apply(current, apply);
            return projection;
        }

        public static async Task<T[]> GetAsync<T, TEvent>(this IEventStore store, string streamName)
            where T : class, new()
            where TEvent : EventRecord
        {
            var r = await store.LoadEventStreamAsync(streamName, 0);
            return r.Events
                .Where(e => e.Event.GetType().BaseType == typeof(TEvent)) //TODO fix!
                .Cast<IEvent>()
                .GroupBy(x => x.SourceId)
                .Select(x => x.Select(e => e.Event).Rehydrate<T>())
                .ToArray();
        }

        public static async Task Publish<T>(this Projector<T> projector, IEvent trigger, string streamName, Func<IEvent[], Task> pub)
            where T : EventRecord, IIntegrationEvent, new()
        {
            var p = await projector.ProjectAsync(streamName);
            var e = new[] { p }
            .ToEnvelopes(trigger.SourceId)
            .ForEach(x => x.Meta.AddMetaData(streamName, trigger))
            .ToArray();
            await pub(e);
        }

        public static async Task Project<T>(this Projector<T> projector, string streamName, Func<T, Task> save)
            where T : class, new()
                => await save(await projector.ProjectAsync(streamName));

        public static async Task Project<T>(this Projector<T> projector, string streamName, ISnapshotStore snapshotManager)
            where T : class, new()
        {
            var p = await projector.ProjectAsync(streamName);
            await snapshotManager.Apply<T>(typeof(T).Name, snap => p);
        }


    }
}
