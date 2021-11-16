using System;
using System.Text.Json;
using System.Threading.Tasks;

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
                        .Conditional(() => ctx.Configuration.GetValue<bool>("FIFFI_DAPR"),
                        s => s.AddFiffiDapr("localcosmos").Tap(x => x.AddChangeFeed(ctx)),
                        s => s.AddFiffiInMemory())
                        .AddApplicationInsightsTelemetry()
                        .AddSingleton(JsonSerializerOptions)
                        .AddSingleton(TypeResolver.FromMap(TypeResolver.GetEventsInNamespace<GameCreated>()))
                        .AddModule(GameModule.Initialize)
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
    public static IServiceCollection Conditional(this IServiceCollection services,
        Func<bool> condition,
        Func<IServiceCollection, IServiceCollection> ifTrue,
        Func<IServiceCollection, IServiceCollection> ifFalse)
     => condition() ? ifTrue(services) : ifFalse(services);

    public static IServiceCollection AddModule<T>(this IServiceCollection services,
        Func<IAdvancedEventStore, ISnapshotStore, Func<IEvent[], Task>, T> f)
        where T : class
        => services.AddSingleton<T>(sp => f(
                  sp.GetRequiredService<IAdvancedEventStore>(),
                  sp.GetRequiredService<ISnapshotStore>(),
                  sp.GetRequiredService<Func<IEvent[], Task>>()
            ));


    public static IServiceCollection AddFiffiInMemory(this IServiceCollection services)
        => services
           .AddSingleton<IAdvancedEventStore, Fiffi.InMemory.InMemoryEventStore>()
           .AddTransient<ISnapshotStore, Fiffi.InMemory.InMemorySnapshotStore>()
           .AddTransient<Func<IEvent[], Task>>(sp => events => sp.GetService<GameModule>().WhenAsync(events));

    public static IServiceCollection AddFiffiDapr(this IServiceCollection services,
        string eventStoreStateStore)
    => services
    .AddEventStore(eventStoreStateStore)
    .AddSnapshotStore(eventStoreStateStore)
    .AddIntegrationEventPublisher("in", "topic");

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
