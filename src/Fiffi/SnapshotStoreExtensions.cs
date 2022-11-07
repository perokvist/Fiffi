namespace Fiffi;

public static class SnapshotStoreExtensions
{
    public static Task<T> Get<T>(this ISnapshotStore snapshotStore)
        where T : class, new()
        => snapshotStore.GetOrCreate<T>(typeof(T).Name);

    public static async Task<T?> GetOrCreate<T>(this ISnapshotStore snapshotStore, string key) where T : class, new()
    {
        var item = await snapshotStore.Get<T>(key);
        if (item == null)
            return new T();

        return item;
    }

    public static Task Apply<T>(this ISnapshotStore snapshotStore, string key, Func<T, T> f)
        where T : class, new()
        => snapshotStore.Apply(key, new T(), f);

    public static Task Apply<T>(this ISnapshotStore snapshotStore, Func<T, T> f)
        where T : class, new()
        => snapshotStore.Apply<T>(typeof(T).Name, f);

    public static Task Apply<T>(this ISnapshotStore snapshotStore, params IEvent[] events)
        where T : class, new()
        => snapshotStore.Apply<T>(view => events.Select(x => x.Event).Apply(view));

    public static Task Apply<T>(this ISnapshotStore snapshotStore, string key, params IEvent[] events)
        where T : class, new()
            => snapshotStore.Apply<T>(key, view => events.Select(x => x.Event).Apply(view));

    public static Task Apply<T>(this ISnapshotStore snapshotStore, string key, IEnumerable<IEvent> events, Func<T, EventRecord, T> f)
    where T : class, new()
        => snapshotStore.Apply<T>(key, v => events.Select(e => e.Event).Apply(v, f));

    public static async Task<T> Get<T>(this ISnapshotStore snapshotStore, string key, Func<T, bool> filter)
        where T : class, new()
    {
        var item = await snapshotStore.GetOrCreate<T>(key);
        if (!filter(item))
            return default;
        return item;
    }

    public static Task Apply<T>(this ISnapshotStore snapshotStore, string key, IEnumerable<EventRecord> events, Func<T, EventRecord, T> f)
        where T : class, new()
        => snapshotStore.Apply<T>(key, v => events.Apply(v, f));

    public static async Task Apply<T>(this ISnapshotStore snapshotStore,
    string key,
    IEnumerable<EventRecord> events,
    Func<T, EventRecord, T> f,
    Func<Task<T>> initialize)
     where T : class
    {
        var item = await snapshotStore.Get<T>(key);
        if (item == null)
            item = await initialize();

        events.Apply(f);
    }
}
