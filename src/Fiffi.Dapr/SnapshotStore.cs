using Dapr.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public class SnapshotStore : ISnapshotStore
    {
        private readonly DaprClient client;
        private readonly ILogger<SnapshotStore> logger;

        public string StoreName { get; set; } = "statestore";

        public SnapshotStore(DaprClient client, ILogger<SnapshotStore> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        public async Task<T> Get<T>(string key)
            where T : class, new()
        {
            var item = await client.GetStateAsync<T>(StoreName, key).AsTask();
            if (item == null)
                return new T();

            return item;
        }

        public async Task Apply<T>(string key, Func<T, T> f)
            where T : class, new()
        {
            var (item, tag) = await client.GetStateAndETagAsync<T>(StoreName, key);
            if (item == null)
            {
                item = new T();
            }
            var newItem = f(item);
            var success = await client.TrySaveStateAsync(StoreName, key, newItem, tag);
            if (!success)
            {
                var ex = new DBConcurrencyException($"item with {key} have been updated");
                logger.LogError(ex, ex.Message);
                throw ex;
            }
        }
    }
}
