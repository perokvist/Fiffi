using Dapr.Client;
using Microsoft.Extensions.Logging;
using Fiffi;
using Dapr.EventStore;
using System.Text.Json;

namespace TTD.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            var jsonOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var client = new DaprClientBuilder()
                .UseJsonSerializationOptions(jsonOptions)
                .Build();

            var loggerFactory = new LoggerFactory();
            var logger = loggerFactory.CreateLogger<DaprEventStore>();

            var store = new Fiffi.Dapr.DaprEventStore(new Dapr.EventStore.DaprEventStore(client, logger), TypeResolver.FromMap(TypeResolver.GetEventsInAssembly<Arrived>()));

            var (time, _) = TTD.Fiffied.App.RunAsync(store, args).GetAwaiter().GetResult();
        }
    }
}
