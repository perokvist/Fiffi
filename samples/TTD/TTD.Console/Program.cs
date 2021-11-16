using Dapr.Client;
using Microsoft.Extensions.Logging;
using Fiffi;
using Dapr.EventStore;
using System.Text.Json;
using Fiffi.InMemory;
using Fiffi.Visualization;

namespace TTD.Console;

class Program
{
    static void Main(string[] args)
    {
        //var jsonOptions = new JsonSerializerOptions()
        //{
        //    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //    PropertyNameCaseInsensitive = true
        //};

        //var client = new DaprClientBuilder()
        //    .UseJsonSerializationOptions(jsonOptions)
        //    .Build();

        //var loggerFactory = new LoggerFactory();
        //var logger = loggerFactory.CreateLogger<DaprEventStore>();

        //var store = new Fiffi.Dapr.DaprEventStore(new Dapr.EventStore.DaprEventStore(client, logger), TypeResolver.FromMap(TypeResolver.GetEventsInAssembly<Arrived>()));

        var (time, events) = TTD.Fiffied.App.RunAsync(new InMemoryEventStore(), args).GetAwaiter().GetResult();
        global::System.Console.WriteLine(events.Draw());

    }
}
