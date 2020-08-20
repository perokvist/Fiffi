using Dapr.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fiffi.Dapr
{
    public static class Extensions
    {
        public static Func<IEvent[], Task> IntegrationPublisher(this IServiceProvider sp, string topicName)
         => async events =>
         {
             //TODO break out in util dapr
             var logger = (ILogger)sp.GetService(typeof(ILogger<DaprClient>));
             var dc = (DaprClient)sp.GetService(typeof(DaprClient));
             foreach (var item in events.OfType<IIntegrationEvent>())
             {
                 logger.LogInformation("publishing event {eventType} to {topicName}", item.GetType().Name, topicName);
                 await dc.PublishEventAsync<object>(topicName, item);
             }
         };

        public static IEnumerable<IEvent> FeedFilter(
            this IEnumerable<JsonDocument> docs,
            Func<string, Type> typeProvider,
            ILogger logger)
        {
            foreach (var d in docs)
            {
                using (logger.BeginScope($"Document {d.RootElement.GetProperty("id")}"))
                {
                    foreach (var e in new[] { d }.FeedFilter(typeProvider))
                    {
                        logger.LogInformation($"Filter included : {e.GetEventName()}");
                        yield return e;
                    }
                }
            }
        }

        public static IEnumerable<IEvent> FeedFilter(this IEnumerable<JsonDocument> docs, Func<string, Type> typeProvider)
            => docs
            .Where(x => !x.RootElement.GetProperty("id").GetString().EndsWith("|head"))
            .Where(x => x.RootElement.GetProperty("id").GetString().Contains("aggregate"))
            .SelectMany(x => JsonSerializer.Deserialize<global::Dapr.EventStore.EventData[]>(x.RootElement.GetProperty("value").GetRawText()))
            .Select(x => (IEvent)JsonSerializer.Deserialize(x.Data, typeProvider(x.EventName)));
    }
}
