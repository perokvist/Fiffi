using Fiffi.Modularization;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.CosmosChangeFeed
{
    public static class Extensions
    {
        public static async Task<ChangeFeedProcessor> CreateProcessorAsync<T>(
            this CosmosClient client,
            string databaseId,
            string leasesContainerId,
            string containerId,
            string processorName,
            string instanceName,
            Func<IReadOnlyCollection<T>, CancellationToken, Task> f)
        {
            var db = client.GetDatabase(databaseId);
            _ = await db.CreateContainerIfNotExistsAsync(leasesContainerId, "/id");
            var leasesContainer = client.GetContainer(databaseId, leasesContainerId);
            var monitoredContainer = client.GetContainer(databaseId, containerId);
            return monitoredContainer
                .GetChangeFeedProcessorBuilder<T>(processorName, (x, ct) => f(x, ct))
                .WithInstanceName(instanceName)
                .WithLeaseContainer(leasesContainer)
                .WithPollInterval(TimeSpan.FromSeconds(1))
                .Build();
        }

        public static IServiceCollection AddChangeFeedSubscription<T, TModule>
            (this IServiceCollection sc,
            IConfiguration configuration,
            Action<SubscriptionOptions> options,
            Func<T, JsonDocument> converter,
            Func<IEnumerable<JsonDocument>, Func<string, Type>, ILogger, IEnumerable<IEvent>> filter)
            where TModule : Module
            => AddChangeFeedSubscription(sc, configuration, options, converter, filter,
                (sp, events) => sp.GetService<TModule>().WhenAsync(events.ToArray()));

        public static IServiceCollection AddChangeFeedSubscription<T>(
            this IServiceCollection sc,
            IConfiguration configuration,
            Action<SubscriptionOptions> options,
            Func<T, JsonDocument> converter,
            Func<IEnumerable<JsonDocument>, Func<string, Type>, ILogger, IEnumerable<IEvent>> filter,
            Func<IServiceProvider, IEnumerable<IEvent>, Task> handler)
            => AddChangeFeedSubscription<T>(
                sc, configuration, options, sp => async (docs, ct) =>
                {
                    var logger = sp.GetService<ILogger<ChangeFeedHostedService>>();

                    try
                    {
                        var typeProvider = sp.GetService<Func<string, Type>>();
                        var convertedDocs = docs.Select(converter);
                        await handler(sp, filter(convertedDocs, typeProvider, logger));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"Fail to proccess events. {ex.Message}");
                        throw;
                    }

                });


        public static IServiceCollection AddChangeFeedSubscription<T>(
            this IServiceCollection sc,
            IConfiguration configuration,
            Action<SubscriptionOptions> options,
            Func<IServiceProvider, Func<IReadOnlyCollection<T>, CancellationToken, Task>> f)
            => sc
            .Tap(x =>
                x.AddOptions<SubscriptionOptions>()
                .Bind(configuration)
                .Configure(options)
                .ValidateDataAnnotations())
            .AddTransient<Func<CosmosClient>>(sp => () =>
            {
                var opt = sp.GetRequiredService<IOptions<SubscriptionOptions>>().Value;
                return new CosmosClient(opt.ServiceUri.ToString(), opt.Key);
            })
            .AddSingleton<Func<CosmosClient, Task<ChangeFeedProcessor>>>(sp => async (client) =>
            {
                var opt = sp.GetRequiredService<IOptions<SubscriptionOptions>>().Value;
                return await client.CreateProcessorAsync(
                    opt.DatabaseName,
                    "leases",
                    opt.ContainerId,
                    opt.ProcessorName,
                    opt.InstanceName,
                    f(sp)
                 );
            })
            .AddHostedService<ChangeFeedHostedService>();
    }
}
