using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.Projections
{
    public static class ProjectionExtensions
    {
        public static Projector<T> Projector<T>(this IEventStore store)
          where T : class, new()
          => new Projector<T>(store);

        public static async Task<long> AppendToStreamAsync(this IEventStore store, string streamName, params IEvent[] events)
        {
            var r = await store.LoadEventStreamAsync(streamName, 0);
            return await store.AppendToStreamAsync(streamName, r.Version, events);
        }

        public static Task<T> GetAsync<T, TStream>(this IEventStore store, IAggregateId id)
            where T : class, new()
         => store.GetAsync<T>(typeof(TStream).Name.AsStreamName(id).StreamName);

        public static async Task<T> GetAsync<T>(this IEventStore store, string streamName)
            where T : class, new()
        {
            var r = await store.LoadEventStreamAsync(streamName, 0);
            var projection = r.Item1.Rehydrate<T>();
            return projection;
        }

        public static async Task Publish<T>(this Projector<T> projector, string streamName, Func<IEvent[], Task> pub)
       where T : class, IEvent, new()
       => await pub(new IEvent[] { await projector.ProjectAsync<T>(streamName) });

    }
}
