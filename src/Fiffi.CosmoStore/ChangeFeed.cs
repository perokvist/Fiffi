using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Azure.Documents.Client;
using System;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore
{
    public static class ChangeFeed
    {
        public static Task<IChangeFeedProcessor> CreateProcessorAsync(
            Uri serviceUri,
            string key,
            string hostName,
            Func<string, Type> typeResolver,
            Func<IEvent[], Task> dispatcher
            )
            => CreateProcessorAsync(serviceUri, key, hostName, "EventStore",
                "Events", new FeedObserverFactory(dispatcher, typeResolver),
                builder => { });

        public static async Task<IChangeFeedProcessor> CreateProcessorAsync(
            Uri serviceUri,
            string key,
            string hostName,
            string databaseName,
            string collectionName,
            Microsoft.Azure.Documents.ChangeFeedProcessor.FeedProcessing.IChangeFeedObserverFactory feedObserverFactory,
            Action<ChangeFeedProcessorBuilder> config)
        {
            await CreateCollectionAsync(serviceUri, key, databaseName, "leases");

            var feedCollectionInfo = new DocumentCollectionInfo()
            {
                DatabaseName = databaseName,
                CollectionName = collectionName,
                Uri = serviceUri,
                MasterKey = key
            };

            var leaseCollectionInfo = new DocumentCollectionInfo()
            {
                DatabaseName = databaseName,
                CollectionName = "leases",
                Uri = serviceUri,
                MasterKey = key
            };

            var builder = new ChangeFeedProcessorBuilder();
            var processor = await builder
                .WithHostName(hostName)
                .WithFeedCollection(feedCollectionInfo)
                .WithLeaseCollection(leaseCollectionInfo)
                .Tap(x => config(x))
                .WithObserverFactory(feedObserverFactory)
                .BuildAsync();
            return processor;
        }

        static async Task CreateCollectionAsync(Uri serviceUri, string key, string databaseName, string collectionName)
        {
            using (var c = new DocumentClient(serviceUri, key))
            {
                _ = await c.
                CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName),
                new Microsoft.Azure.Documents.DocumentCollection { Id = collectionName });
            }
        }
    }
}
