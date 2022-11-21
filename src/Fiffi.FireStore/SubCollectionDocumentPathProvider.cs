using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.FireStore;
public static class DocumentPathProviders
{
    public record StreamPaths(string HeaderPath, string StreamPath, string SnapPath);
    public record StreamContext(string StoreCollection, string Key, bool Writeoperation);

    public static Func<FirestoreDb, StreamContext, Task<StreamPaths>> All() =>
    (store, context) =>
            Task.FromResult(new StreamPaths(
                $"{context.StoreCollection}/{context.Key}|head",
                $"{context.StoreCollection}",
                $"{context.StoreCollection}/{context.Key.SnapshotSufix()}"));

    public static string SnapshotSufix(this string key)
        => key.Pipe(x => x.EndsWith("|snapshot") ? x : $"{x}|snapshot");

public static Func<FirestoreDb, StreamContext, Task<StreamPaths>> SubCollectionAll() =>
         async (store, ctx) =>
         {
             var old = await SubCollectionWithCreate()(store, (ctx.StoreCollection, ctx.Key, ctx.Writeoperation));
             return new StreamPaths(old, $"{old}/events", old.SnapshotSufix());
         };


    public static Func<FirestoreDb, StreamContext, Task<StreamPaths>>
        SubCollectionByPartition() => async (store, ctx) =>
        {
            var old = await SubCollectionWithCreate()(store, (ctx.StoreCollection, ctx.Key, ctx.Writeoperation));
            return new StreamPaths(old, $"{old}/events", old);
        };

    static Func<FirestoreDb, (string StoreCollection, string Key, bool WriteOperation), Task<string>>
        SubCollectionWithCreate() => async (s, x) =>
        {
            var defaultPath = $"{x.StoreCollection}/{x.Key}";

            if (!x.Key.Contains('/'))
                return defaultPath;

            if (!x.WriteOperation)
                return x.Key.Trim('/');

            if (!Uri.TryCreate(x.Key, UriKind.Relative, out var urlKey))
                return x.Key;
            if (urlKey is null)
                return x.Key;

            var segments = urlKey.ToString().Trim('/').Split('/');
            if (segments.Length != 4)
                return x.Key;

            var rootDocPath = new Uri($"{segments[0]}/{segments[1]}", UriKind.Relative);
            var doc = s.Document(rootDocPath.ToString());
            var snap = await doc.GetSnapshotAsync();
            if (!snap.Exists)
                await doc.CreateAsync(new Dictionary<string, string>());
            return x.Key.Trim('/');
        };
}
