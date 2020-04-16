using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Fiffi;
using System.Threading.Tasks;
using Dapr.Client;
using System.Linq;

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
                .ConfigureLogging(b => b.AddConsole())
                .ConfigureWebHostDefaults(webBuilder =>
                    webBuilder.ConfigureServices(services =>
                        services
                            .AddSingleton(TypeResolver.FromMap(TypeResolver.GetEventsInNamespace<GameCreated>()))
                            .AddSingleton(sp => GameModule.Initialize(
                                new InMemoryEventStore(), async events => {
                                    var dc = sp.GetRequiredService<DaprClient>();
                                    var m = sp.GetRequiredService<GameModule>();

                                    await Task.WhenAll(events.Select(e => dc.PublishEventAsync<object>("in", e)));

                                    await Task.WhenAll(events
                                        .OfType<IIntegrationEvent>()
                                        .Select(e => dc.PublishEventAsync("out", e)));

                                    //await m.WhenAsync(events); // Local only !
                                }))
                            .AddLogging()
                            .AddControllers()
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
                        });
                    })
                );
    }
}
