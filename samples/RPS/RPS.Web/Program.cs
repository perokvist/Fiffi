using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Fiffi;
using Fiffi.Dapr;
using Newtonsoft.Json.Linq;
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
                            .AddTransient<ISnapshotStore, Fiffi.Dapr.SnapshotStore>()
                            .AddSingleton(sp => GameModule.Initialize(
                                sp.GetRequiredService<IAdvancedEventStore>(), 
                                sp.GetRequiredService<ISnapshotStore>(),
                                Fiffi.Dapr.Extensions.IntegrationPublisher(sp, "in")
                                ))
                            .AddChangeFeedSubscription<JToken, GameModule>(
                                ctx.Configuration,
                                opt =>
                                { //TODO use dapr secrets + ext to map secrets -> opt | or via config ?
                                    opt.InstanceName = "RPS.Web";
                                    opt.ServiceUri = new System.Uri("https://localhost:8081");
                                    opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                                    opt.DatabaseName = "dapr";
                                    opt.ContainerId = "events";
                                    opt.ProcessorName = "eventsubscription";
                                },
                                token => JsonDocument.Parse(token.ToString()),
                                Fiffi.Dapr.Extensions.FeedFilter,
                                feedBuilder => { })
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
