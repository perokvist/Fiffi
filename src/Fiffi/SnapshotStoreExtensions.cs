using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class SnapshotStoreExtensions
    {
        public static Task<T> Get<T>(this ISnapshotStore snapshotStore)
            where T : class, new()
            => snapshotStore.Get<T>(typeof(T).Name);

        public static Task Apply<T>(this ISnapshotStore snapshotStore, Func<T, T> f)
            where T : class, new()
            => snapshotStore.Apply<T>(typeof(T).Name, f);

    }
}
