using Dapr.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class SnapshotStore : ISnapshotStore
    {
        private readonly DaprClient client;
        private readonly ILogger<SnapshotStore> logger;

        public string StoreName { get; set; } = "statestore";

        public Func<string, Dictionary<string, string>> MetaProvider { get; set; } = key => new Dictionary<string, string>();

        public SnapshotStore(DaprClient client, ILogger<SnapshotStore> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task<T?> Get<T>(string key)
            where T : class
        {
            var meta = MetaProvider(key);
            var item = await client.GetStateAsync<T>(StoreName, key, metadata: meta);
            if (item == null)
                return null;

            return item;
        }

        public Task Apply<T>(string key, T defaultValue, Func<T, T> f)
            where T : class
            => Apply<T>(key, f, () => defaultValue);

        public async Task Apply<T>(string key, Func<T, T> f, Func<T> c)
        {
            var meta = MetaProvider(key);
            var (item, tag) = await client.GetStateAndETagAsync<T>(StoreName, key, metadata: meta);
            if (item == null)
            {
                item = c();
            }
            var newItem = f(item);


            if (item is IEquatable<T>)
            {
                if (item.Equals(newItem))
                    return;
            }

            var success = await client.TrySaveStateAsync(StoreName, key, newItem, tag, metadata: meta);
            if (!success)
            {
                var ex = new DBConcurrencyException($"item with {key} have been updated");
                logger.LogError(ex, ex.Message);
                throw ex;
            }
        }
    }
}
