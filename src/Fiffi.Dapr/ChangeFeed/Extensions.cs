using Dapr.EventStore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Fiffi.Dapr.ChangeFeed
{
    public static class Extensions
    {

        public static Func<IEnumerable<IEvent>> FeedFilter(
            this IEnumerable<JsonDocument> docs,
            Func<string, Type> typeProvider,
            ILogger logger, JsonSerializerOptions options) => FeedFilter(docs, typeProvider, logger, logLevel: LogLevel.Information);

        public static Func<IEnumerable<IEvent>> FeedFilter(
            this IEnumerable<JsonDocument> docs,
            Func<string, Type> typeProvider,
            ILogger logger, LogLevel logLevel = LogLevel.Information,
            JsonSerializerOptions options = null)
        {
            var toEvent = DaprEventStore.ToEvent();

            return () =>
            {
                logger.Log(logLevel, $"Recieved : {docs.Count()} event(s).");
                var eventsToProcess = docs
                         .Where(Filter())
                         .Select(ToEventData)
                         .Select(ed => toEvent(ed, typeProvider(ed.EventName), options ?? new()));

                logger.Log(logLevel, $"{eventsToProcess.Count()} event(s) to process.");

                return eventsToProcess;
            };
        }

        public static Func<JsonDocument, bool> Filter(string aggregateStreamIdentifier = "aggregate") =>
            d => new[] { d }
            .Where(x => !x.RootElement.GetProperty("id").GetString().EndsWith("|head"))
            .Where(x => x.RootElement.GetProperty("id").GetString().Contains(aggregateStreamIdentifier))
            .Any();

        public static EventData ToEventData(this JsonDocument doc)
        {
            var value = doc.RootElement.GetProperty("value").GetRawText();
            var eventData = JsonSerializer.Deserialize<EventData>(value);
            return eventData;
        }

    }
}
