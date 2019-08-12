using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fiffi;
using Fiffi.CosmoStore.Configuration;
using Fiffi.Visualization;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
                    sc.AddModule<SampleModule, SampleOptions>(
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
                .Configure(builder => { });

        public class SampleModule
        {
            private readonly Func<IEvent[], Task> publish;

            public SampleModule(Func<IEvent[], Task> publish)
            {
                this.publish = publish;
            }

            public static SampleModule Initialize(IEventStore eventStore, ILoggerFactory loggerFactory)
            {
                var logger = loggerFactory.CreateLogger<SampleModule>();
                var ep = new EventProcessor();
                ep.Register<TestEvent>(e =>
                {
                    e.Meta.ForEach(m => logger.LogInformation($"{m.Key} : {m.Value}"));
                    return Task.CompletedTask;
                });

                return new SampleModule(events =>
                {
                    logger.LogInformation($"{nameof(SampleModule)} got {events.Length} events");
                    logger.LogInformation(events.Draw());
                    return ep.PublishAsync(events);
                });
            }


            public Task WhenAsync(params IEvent[] events) => this.publish(events);

        }

        public class SampleOptions : ModuleOptions
        { }

        public class TestEvent : IEvent
        {
            public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
            public string SourceId { get; set; }
        }
    }
}
