using System.Collections.Concurrent;

namespace Fiffi.InMemory;

public class InMemorySnapshotStore : ISnapshotStore
{
    readonly IDictionary<string, object> store = new ConcurrentDictionary<string, object>();

    public async Task Apply<T>(string key, T defaultValue, Func<T, T> f) where T : class
    {
        var currentValue = (await Get<T>(key)) ?? defaultValue;
        var newValue = f(currentValue);

        store[key] = newValue;
    }

    public Task<T?> Get<T>(string key) where T : class
        => Task.FromResult(store.ContainsKey(key) ? (T)store[key] : null);
}
