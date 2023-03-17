using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.OpenApi.Models;

using Newtonsoft.Json.Linq;

using Fiffi;
using Fiffi.Dapr;
using Fiffi.Serialization;
using Fiffi.CosmosChangeFeed;
using Fiffi.AspNetCore;

namespace RPS.Web;

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
                        .Conditional(ctx.Configuration.FiffiDaprEnabled(),
                            s => s
                            .AddFiffi(opt => {
                                opt.TypeResolver = TypeResolver.GetEventsInNamespace<GameCreated>();
                                opt.JsonSerializerOptions = JsonSerializerOptions;
                            })
                            .AddTransient<BindingPublisher>()
                            .AddDaprEventStore()
                            .AddDaprSnapshotStore(), 
                            s => s.AddFiffiInMemory())
                        .AddFiffiModule(GameModule.Initialize)
                        .Conditional(ctx.Configuration.DaprEnabled(),
                            s => s.AddInMemoryEventSubscribers(sp => sp.GetRequiredService<BindingPublisher>().Publish),
                            s => s.AddInMemoryEventSubscribers())
                        .AddApplicationInsightsTelemetry()
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
    public static IServiceCollection AddFiffiDapr(this IServiceCollection services,
        string eventStoreStateStore)
    => services
    .AddDaprEventStore(eventStoreStateStore)
    .AddDaprSnapshotStore(eventStoreStateStore)
    .AddDaprPubSubIntegrationEventPublisher("in", "topic");

    public static IServiceCollection AddChangeFeed(this IServiceCollection services, WebHostBuilderContext ctx)
        => services
           .AddChangeFeedSubscription<JToken, GameModule>(
               ctx.Configuration,
               opt =>
               { //TODO use dapr secrets + ext to map secrets -> opt | or via config ?
                   opt.InstanceName = "RPS.Web";
                   opt.ServiceUri = new System.Uri("https://localhost:8081");
                   opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                   opt.DatabaseName = "dapr";
                   opt.ContainerId = "dapreventstore";
                   opt.ProcessorName = "eventsubscription";
               },
               token => JsonDocument.Parse(token.ToString()),
               Fiffi.Dapr.ChangeFeed.Extensions.FeedFilter,
               feedBuilder => { });
}
