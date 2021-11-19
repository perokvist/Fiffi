using System.Collections.Concurrent;

namespace Fiffi.InMemory;

public class InMemorySnapshotStore : ISnapshotStore
{
    readonly IDictionary<string, object> store = new ConcurrentDictionary<string, object>();

    public async Task Apply<T>(string key, Func<T, T> f) where T : class, new()
    {
        var currentValue = await Get<T>(key);
        var newValue = f(currentValue);

        if (currentValue is IEquatable<T>)
            if (currentValue.Equals(newValue))
                return;

        store[key] = newValue;
    }

    public Task<T> Get<T>(string key) where T : class, new()
        => Task.FromResult(store.ContainsKey(key) ? (T)store[key] ?? new T() : new T().Tap(x => store.Add(key, x)));
}
