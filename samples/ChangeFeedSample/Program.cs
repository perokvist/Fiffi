using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Fiffi.CosmosChangeFeed.Configuration;
using Fiffi;
using System;
using Fiffi.Testing;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

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
                    .AddChangeFeedSubscription(
                        ctx.Configuration,
                        opt =>
                        {
                            opt.HostName = nameof(ChangeFeedSample);
                            opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                            opt.ServiceUri = new Uri("https://localhost:8081");
                            opt.CollectionName = "events";
                            opt.DatabaseName = "dapr";
                            opt.TypeResolver = TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(TestEvent)));
                        },
                        sp => docs => Task.CompletedTask)
                    .AddMvc())
                .Configure(builder => builder.UseMvcWithDefaultRoute());


    }
}
