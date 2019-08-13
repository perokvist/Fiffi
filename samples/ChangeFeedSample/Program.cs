using System;
using Fiffi;
using Fiffi.CosmoStore.Configuration;
using Fiffi.Testing;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Fiffi.CosmoStore.Configuration.Extensions;

namespace ChangeFeedSample
{
    public class Program
    {
        public static void Main(string[] args)
        => CreateWebHostBuilder(args).Build().Run();

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging(lb => lb.AddConsole())
                .ConfigureServices((ctx, sc) =>
                    sc
                    .Tap(x => x.AddMvc())
                    .AddModule<SampleModule, SampleOptions>(
                        ctx.Configuration,
                        (sp, es) => SampleModule.Initialize(es, sp.GetRequiredService<ILoggerFactory>()),
                        opt =>
                        {
                            opt.HostName = nameof(ChangeFeedSample);
                            opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                            opt.ServiceUri = new Uri("https://localhost:8081");
                            opt.TypeResolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent)));
                        })
                    .AddChangeFeedSubscription<SampleModule, SampleOptions>((sp, module, logger) => events =>
                    {
                        logger.LogInformation($"Change feed hosted service got {events.Length} events");
                        return module.WhenAsync(events);
                    }))
                .Configure(builder => builder.UseMvcWithDefaultRoute());


    }
}
