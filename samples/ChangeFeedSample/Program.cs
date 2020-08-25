using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Fiffi.CosmosChangeFeed;
using Newtonsoft.Json.Linq;
using System.Text.Json;

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
                    .AddChangeFeedSubscription<JToken>(
                                ctx.Configuration,
                                opt =>
                                {
                                    opt.InstanceName = "ProcessorSample";
                                    opt.ServiceUri = new System.Uri("https://localhost:8081");
                                    opt.Key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                                    opt.DatabaseName = "dapr";
                                    opt.ContainerId = "events";
                                    opt.ProcessorName = "sample";
                                },
                                token => JsonDocument.Parse(token.ToString()),
                                Fiffi.Dapr.Extensions.FeedFilter,
                                (sp, events) => Task.CompletedTask,
                                feedBuilder => { })
                    .AddMvc())
                .Configure(builder => builder.UseMvcWithDefaultRoute());


    }
}
