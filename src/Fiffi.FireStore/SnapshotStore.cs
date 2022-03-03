using Fiffi.Serialization;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi.FireStore
{
    public class SnapshotStore : ISnapshotStore
    {
        private readonly FirestoreDb store;
        private JsonSerializerOptions options;

        public string StoreCollection { get; set; } = "snapshots";
        public Func<FirestoreDb, (string StoreCollection, string Key), Task<string>> DocumentPathProvider =
            (store, x) => Task.FromResult($"{x.StoreCollection}/{x.Key}");

        public SnapshotStore(FirestoreDb store, JsonSerializerOptions options)
        {
            this.store = store;
            this.options = options;
        }

        public async Task Apply<T>(string key, Func<T, T> f) where T : class, new()
        {
            var snap = await Get<T>(key);
            var newSnap = f(snap);

            if (snap is IEquatable<T>)
            {
                if (snap.Equals(newSnap))
                    return;
            }

            var snapRef = store.Document(await DocumentPathProvider(store, (StoreCollection, key)));
            var snapMap = newSnap.ToMap(options);
            await snapRef.SetAsync(snapMap);
        }

        public async Task<T> Get<T>(string key) where T : class, new()
        {
            var snapRef = store.Document(await DocumentPathProvider(store, (StoreCollection, key)));
            var snap = await snapRef
                .GetSnapshotAsync();

            if (!snap.Exists)
                return new T();

            var snapMap = snap.ConvertTo<Dictionary<string, object>>();

            return snapMap.ToObject<T>(this.options);
        }
    }
}
