using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public static class SnapshotManagerExtensions
    {
        public static Task<T> Get<T>(this ISnapshotManager snapshotManager)
            where T : class, new()
            => snapshotManager.Get<T>(typeof(T).Name);

        public static Task Apply<T>(this ISnapshotManager snapshotManager, Func<T, T> f)
            where T : class, new()
            => snapshotManager.Apply<T>(typeof(T).Name, f);
    }
}
