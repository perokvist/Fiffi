using Fiffi.Projections;

namespace Fiffi;

public static class SnapshotStoreExtensions
{
    public static Task<T> Get<T>(this ISnapshotStore snapshotStore)
        where T : class, new()
        => snapshotStore.GetOrCreate<T>(typeof(T).Name);



    public static Task<T> GetOrCreate<T>(this ISnapshotStore snapshotStore, string key) where T : class, new()
        => GetOrCreate(snapshotStore, key, () => new T());

    public static async Task<T> GetOrCreate<T>(
        this ISnapshotStore snapshotStore,
        string key,
        Func<T> factory) where T : class
    {
        var item = await snapshotStore.Get<T>(key);
        if (item == null)
            return factory();

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
        => snapshotStore.Apply<T>(key, state => events.Select(e => e.Event).Apply(state, f));

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
        => snapshotStore.Apply<T>(key, state => events.Apply(state, f));

    public static Task Apply<T>(this ISnapshotStore snapshotStore, string key, T initialValue,
        IEnumerable<EventRecord> events, Func<T, EventRecord, T> f)
        where T : class
        => snapshotStore.Apply(key, initialValue, state => events.Apply(state, f));

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

        await snapshotStore.Apply(key, item, events, f);
    }

    public static Task ApplyLazy<T>(this ISnapshotStore snapshotStore,
     IEventStore store, IEnumerable<EventRecord> events,
     string cacheKey, string streamName,
     T initialValue,
     Func<T, EventRecord, T> apply)
     where T : class
     => snapshotStore.Apply(cacheKey, events, apply,
         () => store.GetAsync(streamName, initialValue, apply));

    public static async Task<T> GetLazy<T>(this ISnapshotStore snapshotStore,
        IEventStore store, string cacheKey, string streamName,
        T initialValue, Func<T, EventRecord, T> apply)
        where T : class
    {
        var item = await snapshotStore.Get<T>(cacheKey);
        if (item == null)
        {
            item = await store.GetAsync(streamName, initialValue, apply);
        }
        return item;
    }

}
