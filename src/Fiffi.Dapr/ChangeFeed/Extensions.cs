using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Fiffi.Dapr.ChangeFeed
{
    public static class Extensions
    {

        public static Func<IEnumerable<JsonDocument>, IEnumerable<IEvent>> FeedFilter(
            Func<string, Type> typeProvider,
            ILogger logger, JsonSerializerOptions options) => FeedFilter(typeProvider, logger, logLevel: LogLevel.Information);

        public static Func<IEnumerable<JsonDocument>, IEnumerable<IEvent>> FeedFilter(
            Func<string, Type> typeProvider,
            ILogger logger, LogLevel logLevel = LogLevel.Information,
            JsonSerializerOptions options = null)
        {
            var toEvent = DaprEventStore.ToEvent();

            return docs =>
            {
                logger.Log(logLevel, $"Recieved : {docs.Count()} event(s).");
                docs.ForEach(d => logger.Log(logLevel, $"Recieved : {d.RootElement.GetProperty("id").GetString()}"));

                var eventsToProcess = docs
                         .Where(Filter())
                         .Select(ToEventData)
                         .Select(ed => toEvent(ed, typeProvider(ed.EventName), options ?? new()))
                         .ToArray();

                logger.Log(logLevel, $"{eventsToProcess.Length} event(s) to process.");

                return eventsToProcess;
            };
        }

        public static Func<JsonDocument, bool> Filter(string aggregateStreamIdentifier = "aggregate") =>
            d => new[] { d }
            .Where(x => !x.RootElement.GetProperty("id").GetString().EndsWith("|head"))
            .Where(x => !x.RootElement.GetProperty("id").GetString().EndsWith("|snapshot"))
            .Where(x => x.RootElement.GetProperty("id").GetString().Contains(aggregateStreamIdentifier))
            .Any();

        public static global::Dapr.EventStore.EventData ToEventData(this JsonDocument doc)
        {
            var value = doc.RootElement.GetProperty("value").GetRawText();
            var eventData = JsonSerializer.Deserialize<global::Dapr.EventStore.EventData>(value);
            return eventData;
        }

    }
}
