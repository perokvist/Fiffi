using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.OpenApi.Models;
using Fiffi;
using Fiffi.Dapr;
using System.Text.Json;
using Fiffi.Serialization;

namespace RPS.Web
{
    public class Program
    {
        readonly static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
                                .Tap(x => x.Converters.Add(new EventRecordConverter()))
                                .Tap(x => x.Converters.Add(new PlayerTupleConverter()))
                                .Tap(x => x.PropertyNameCaseInsensitive = true);

        public static void Main(string[] args)
            => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.ConfigureServices((ctx, services) =>
                        services
                            .Tap(s =>
                            {
                                if (ctx.Configuration.GetValue<bool>("FIFFI-DAPR"))
                                    s.AddFiffiDapr("statestore");
                                else
                                    s.AddFiffiInMemory();
                            })
                            .AddApplicationInsightsTelemetry()
                            .AddSingleton(JsonSerializerOptions)
                            .AddSingleton(TypeResolver.FromMap(TypeResolver.GetEventsInNamespace<GameCreated>()))
                            .AddSingleton(sp => GameModule.Initialize(
                                sp.GetRequiredService<IAdvancedEventStore>(),
                                sp.GetRequiredService<ISnapshotStore>(),
                                events => sp.GetService<GameModule>().WhenAsync(events)
                                ))
                            .AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "RPS Game", Version = "v1" }))
                            .AddLogging(b => b.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Information))
                            .AddMvc()
                            .AddDapr(client =>
                            {
                                client.UseJsonSerializationOptions(JsonSerializerOptions);
                                var endpoint = ctx.Configuration.GetValue<string>("DAPR_GRPC_ENDPOINT");
                                if (!string.IsNullOrWhiteSpace(endpoint))
                                    client.UseGrpcEndpoint(endpoint);
                            })
                    )
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseCloudEvents();
                        app.UseAuthorization();
                        app.UseSwagger();
                        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json",
                                         "RPS Game v1"));
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapSubscribeHandler();
                            endpoints.MapControllers();
                            endpoints.MapDefaultControllerRoute();
                        });
                        app.UseWelcomePage();
                    })
                );
    }

    public static class Extensions
    {
        public static IServiceCollection AddFiffiInMemory(this IServiceCollection services)
            => services
               .AddSingleton<IAdvancedEventStore, Fiffi.InMemory.InMemoryEventStore>()
               .AddTransient<ISnapshotStore, Fiffi.InMemory.InMemorySnapshotStore>();


        public static IServiceCollection AddFiffiDapr(this IServiceCollection services,
            string eventStoreStateStore)
        => services
        .AddSingleton<global::Dapr.EventStore.DaprEventStore>()
        .AddSingleton<IAdvancedEventStore, DaprEventStore>()
        .Configure<global::Dapr.EventStore.DaprEventStore>(x => x.StoreName = eventStoreStateStore)
        .AddTransient<ISnapshotStore, Fiffi.Dapr.SnapshotStore>()
        .AddSingleton(sp => GameModule.Initialize(
                            sp.GetRequiredService<IAdvancedEventStore>(),
                            sp.GetRequiredService<ISnapshotStore>(),
                            events => sp.GetService<GameModule>().WhenAsync(events) //TODO fix :)
                            //Fiffi.Dapr.Extensions.IntegrationPublisher(sp, "in")
                            ));

        //public static IServiceCollection ChangeFeed(this IServiceCollection services, WebHostBuilderContext ctx)
        //    => services
        //       .AddChangeFeedSubscription<JToken, GameModule>(
        //           ctx.Configuration,
        //           opt =>
        //           { //TODO use dapr secrets + ext to map secrets -> opt | or via config ?
        //               opt.InstanceName = "RPS.Web";
        //               opt.ServiceUri = new System.Uri("https://localhost:8081");
        //               opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        //               opt.DatabaseName = "dapr";
        //               opt.ContainerId = "events";
        //               opt.ProcessorName = "eventsubscription";
        //           },
        //           token => JsonDocument.Parse(token.ToString()),
        //           Fiffi.Dapr.Extensions.FeedFilter,
        //           feedBuilder => { });
    }
}
