using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.InMemory
{
    public class InMemorySnapshotStore : ISnapshotStore
    {
        readonly IDictionary<string, object> store = new ConcurrentDictionary<string, object>();

        public async Task Apply<T>(string key, Func<T, T> f) 
        {
            var currentValue = await Get<T>(key);
            var newValue = f(currentValue.GetOrDefault(default));
            store[key] = newValue;
        }

        public Task<Maybe<T>> Get<T>(string key)
        {
            if (store.ContainsKey(key))
            {
                var v = (T)store[key];
                if (v != null)
                    return Task.FromResult(Maybe<T>.Some(v));
            }

            return Task.FromResult(Maybe<T>.None);
        }
    }
}
