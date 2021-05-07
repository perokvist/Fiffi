using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Fiffi.CosmosChangeFeed;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System;
using System.Linq;

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
                    .AddSingleton(Fiffi.TypeResolver.Default())
                    .AddChangeFeedSubscription<JToken>(
                                ctx.Configuration,
                                opt =>
                                {
                                    opt.InstanceName = "DaprProcessorSample";
                                    opt.ServiceUri = new System.Uri("https://localhost:8081");
                                    opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                                    opt.DatabaseName = "dapr";
                                    opt.ContainerId = "dapreventstore";
                                    opt.ProcessorName = "daprsample";
                                },
                                token => JsonDocument.Parse(token.ToString()),
                                Fiffi.Dapr.ChangeFeed.Extensions.FeedFilter,
                                (sp, events) => {
                                    events.ToArray();
                                    return Task.CompletedTask;
                                },
                                feedBuilder => { })
                                .AddMvc(opt => opt.EnableEndpointRouting = false))
                                .Configure(app =>
                                {
                                    app.UseMvc();
                                    app.UseWelcomePage();
                                });


    }
}
