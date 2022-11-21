using Fiffi.Serialization;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using static Fiffi.FireStore.DocumentPathProviders;

namespace Fiffi.FireStore;

public class SnapshotStore : ISnapshotStore
{
    private readonly FirestoreDb store;
    private JsonSerializerOptions options;

    public bool ImmutableSnapshots { get; set; } = true;
    public string StoreCollection { get; set; } = "snapshots";

    public Func<FirestoreDb, StreamContext, Task<StreamPaths>> DocumentPathProvider = All();

    public SnapshotStore(FirestoreDb store, JsonSerializerOptions options)
    {
        this.store = store;
        this.options = options;
    }

    public async Task Apply<T>(string key, T defaultValue, Func<T, T> f) where T : class
    {
        var snap = await Get<T>(key) ?? defaultValue;
        var newSnap = f(snap);

        if (ImmutableSnapshots && snap is IEquatable<T>)
        {
            if (snap.Equals(newSnap))
                return;
        }

        var snapRef = store.Document((await DocumentPathProvider(store, new(StoreCollection, key, true))).SnapPath);
        var snapMap = newSnap.ToMap(options);
        await snapRef.SetAsync(snapMap);
    }

    public async Task<T?> Get<T>(string key) where T : class
    {
        var path = await DocumentPathProvider(store, new(StoreCollection, key, false));
        var snapRef = store.Document(path.SnapPath);
        var snap = await snapRef
            .GetSnapshotAsync();

        if (!snap.Exists)
            return null;

        var snapMap = snap.ConvertTo<Dictionary<string, object>>();

        return snapMap.ToObject<T>(this.options);
    }
}
