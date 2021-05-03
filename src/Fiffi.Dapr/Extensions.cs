using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public static class Extensions
    {

        public static IServiceCollection AddIntegrationEventPublisher(this IServiceCollection services, string pubsubname, string topicName)
            => services.AddSingleton(sp => sp.IntegrationPublisher(pubsubname, topicName));

        public static Func<IEvent[], Task> IntegrationPublisher(
            this IServiceProvider sp, string pubsubname, string topicName, LogLevel logLevel = LogLevel.Information)
        {
            var logger = (ILogger)sp.GetService(typeof(ILogger<DaprClient>));
            var dc = (DaprClient)sp.GetService(typeof(DaprClient));
            return async events =>
            {
                foreach (var item in events.OfType<IIntegrationEvent>())
                {
                    logger.Log(logLevel, "publishing event {eventType} to {topicName}", item.GetType().Name, topicName);
                    await dc.PublishEventAsync<object>(pubsubname, topicName, item);
                }
            };
        }

        public static IServiceCollection AddEventStore(this IServiceCollection services, string storeName = "statestore") =>
           services.AddSingleton(sp => new global::Dapr.EventStore.DaprEventStore(
                    sp.GetRequiredService<DaprClient>(),
                    sp.GetRequiredService<ILogger<global::Dapr.EventStore.DaprEventStore>>())
                    .Tap(x => x.StoreName = storeName))
                    .AddSingleton<IAdvancedEventStore, DaprEventStore>();

        public static IServiceCollection AddSnapshotStore(this IServiceCollection services, string storeName = "statestore") =>
            services.AddSingleton(sp => new SnapshotStore(
                    sp.GetRequiredService<DaprClient>(),
                    sp.GetRequiredService<ILogger<SnapshotStore>>())
                    .Tap(x => x.StoreName = storeName));
    }
}
