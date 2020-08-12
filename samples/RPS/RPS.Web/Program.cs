using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Fiffi;
using System.Threading.Tasks;
using System.Linq;
using Fiffi.Dapr;
using Newtonsoft.Json.Linq;
using System;
using System.Text.Json;
using Fiffi.CosmosChangeFeed;

namespace RPS.Web
{
    public class Program
    {
        public static void Main(string[] args)
            => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.ConfigureServices((ctx, services) =>
                        services
                            .AddSingleton(TypeResolver.FromMap(TypeResolver.GetEventsInNamespace<GameCreated>()))
                            .AddSingleton<global::Dapr.EventStore.DaprEventStore>()
                            .AddSingleton<IAdvancedEventStore, DaprEventStore>()
                            .AddSingleton(sp => GameModule.Initialize(
                                sp.GetRequiredService<IAdvancedEventStore>(), events => Task.CompletedTask))
                            .AddChangeFeedSubscription<JToken>(
                                ctx.Configuration,
                                opt => { //TODO use dapr secrets + ext to map secrets -> opt | or via config ?
                                    opt.InstanceName = "RPS.Web";
                                    opt.ServiceUri = new System.Uri("https://localhost:8081");
                                    opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                                    opt.DatabaseName = "dapr";
                                    opt.ContainerId = "events";
                                    opt.ProcessorName = "eventsubscription";
                                },
                                sp => async (docs, ct) => {
                                    var logger = sp.GetService<ILogger<ChangeFeedHostedService>>();
                                    var typeProvider = sp.GetService<Func<string, Type>>();
                                    var module = sp.GetService<GameModule>();
                                    var events = docs
                                        .Select(x => JsonDocument.Parse(x.ToString()))
                                        .FeedFilter(sp.GetService<Func<string, Type>>());
                                    ct.ThrowIfCancellationRequested();
                                    logger.LogInformation($"Feed subscription dispatches {events.Count()} events from {docs.Count} changes. " +
                                        $" # {string.Join(',', events.Select(e => e.GetEventName()).ToArray())}" +
                                        $" ## {string.Join(',', docs.Select(e => e.Value<string>("id")).ToArray())}");
                                    await module.WhenAsync(events.ToArray());
                                })
                            .AddLogging()
                            .AddMvc()
                            .AddDapr()
                    )
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseCloudEvents();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapSubscribeHandler();
                            endpoints.MapControllers();
                            endpoints.MapDefaultControllerRoute();
                        });
                    })
                );
    }
}
