using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Fiffi;
using System.Threading.Tasks;
using Dapr.Client;
using System.Linq;
using Fiffi.Dapr;

namespace RPS.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.ConfigureServices(services =>
                        services
                            .AddSingleton(TypeResolver.FromMap(TypeResolver.GetEventsInNamespace<GameCreated>()))
                            .AddSingleton<global::Dapr.EventStore.DaprEventStore>()
                            .AddSingleton<IAdvancedEventStore, DaprEventStore>()
                            .AddSingleton(sp => GameModule.Initialize(
                                sp.GetRequiredService<IAdvancedEventStore>(), async events => {
                                    var logger = sp.GetRequiredService<ILogger<Program>>();
                                    var env = sp.GetRequiredService<IWebHostEnvironment>();
                                    var m = sp.GetRequiredService<GameModule>();

                                    //if (env.IsEnvironment("local"))
                                    //{
                                    //    logger.LogInformation("Local When.");
                                    //    await m.WhenAsync(events); // Local non dapr only !
                                    //    return;
                                    //}

                                    var dc = sp.GetRequiredService<DaprClient>();

                                    foreach (var @event in events)
                                    {
                                        logger.LogInformation("Publishing {eventName}", @event.GetType().Name);
                                        await dc.PublishEventAsync<object>("in", @event);
                                    }

                                    //await Task.WhenAll(events
                                    //    .OfType<IIntegrationEvent>()
                                    //    .Select(e => dc.PublishEventAsync("out", e)));

                                }))
                            .AddLogging()
                            .AddMvc()
                            //.AddControllers()
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
