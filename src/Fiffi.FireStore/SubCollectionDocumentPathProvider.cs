using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.FireStore;
public static class DocumentPathProviders
{
    public static Func<FirestoreDb, (string StoreCollection, string Key), Task<string>> SubCollection()
        => async (s ,x ) => {
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
