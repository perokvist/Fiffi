using Fiffi;
using Fiffi.AspNetCore.Testing;
using Fiffi.CosmosChangeFeed;
using Fiffi.Dapr;
using Fiffi.Testing;
using Fiffi.Visualization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RPS.Web.Tests;

public class BindingPublisherTests
{
    ITestContext context;
    ITestOutputHelper helper;
    IHost host;

    public BindingPublisherTests(ITestOutputHelper outputHelper)
    {
        Environment.SetEnvironmentVariable("DAPR_GRPC_PORT", "50006");

        helper = outputHelper;

        (host, context) = Web.Program
            .CreateHostBuilder(Array.Empty<string>())
            .CreateTestContextAndHostFromServices(services =>
                    services
                    .Remove(services.SingleOrDefault(x => x.ImplementationType == typeof(ChangeFeedHostedService))),
                    (sp, store, pub) => GameModule.Initialize(store, sp.GetRequiredService<ISnapshotStore>(), pub),
                    sp => sp.GetRequiredService<BindingPublisher>()
                    .Tap(x => x.MetaProvider = BindingPublisher.AzureProvider())
                    .Tap(x => x.BindingName = "datastorage")
                    .Publish
            );
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GameEndedPublishesGamePlayed()
    {
        await host.StartAsync();
        var client = host
            .GetTestClient();

        //Given
        var gameId = Guid.NewGuid();
        var aggregateId = new AggregateId(gameId);

        //When
        await context.WhenAsync(
            new CreateGame()
            {
                GameId = gameId,
                PlayerId = "tester",
                Rounds = 1,
                Title = "Test Game"
            });

        //Then  
        context.Then((events, visual) =>
        {
            this.helper.WriteLine(visual);
            Assert.True(events.Happened<GameCreated>());
        });
    }

}
