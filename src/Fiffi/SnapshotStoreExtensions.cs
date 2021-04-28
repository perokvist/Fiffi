using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class SnapshotStoreExtensions
    {
        public static async Task<T> Get<T>(this ISnapshotStore snapshotStore)
            where T : class, new()
        {
            var r = await snapshotStore.Get<T>(typeof(T).Name);
            return r.GetOrDefault(default);
        }

        public static Task Apply<T>(this ISnapshotStore snapshotStore, Func<T, T> f)
            where T : class, new()
            => snapshotStore.Apply<T>(typeof(T).Name, f);

        public static Task Apply<T>(this ISnapshotStore snapshotStore, params IEvent[] events)
            where T : class, new()
            => snapshotStore.Apply<T>(view => events.Select(x => x.Event).Apply(view));

        public static Task Apply<T>(this ISnapshotStore snapshotStore, string key, params IEvent[] events)
            where T : class, new()
                => snapshotStore.Apply<T>(key, view => events.Select(x => x.Event).Apply(view));
    }
}
