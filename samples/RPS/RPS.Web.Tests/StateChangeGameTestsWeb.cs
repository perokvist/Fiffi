using Fiffi;
using Fiffi.CosmosChangeFeed;
using Fiffi.InMemory;
using Fiffi.Testing;
using Fiffi.Visualization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RPS.Web.Tests
{
    public class StateChangeGameTestsWeb
    {
        IHostBuilder hostBuilder;
        ITestContext context;
        ITestOutputHelper helper;

        public StateChangeGameTestsWeb(ITestOutputHelper outputHelper)
        {
            this.context = TestContextBuilder.Create<InMemoryEventStore, GameModule>((store, pub) =>
            {
                this.helper = outputHelper;
                var module = GameModule.Initialize(store, new InMemorySnapshotStore(), pub);
                hostBuilder = RPS.Web.Program.CreateHostBuilder(Array.Empty<string>())
                .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureTestServices(services =>
                    services
                        .AddSingleton(module)
                        .Remove(services.SingleOrDefault(x => x.ImplementationType == typeof(ChangeFeedHostedService)))
                ));
                return module;
            });
        }

        [Fact]
        public async Task JoinGameAsync()
        {
            var host = await hostBuilder.StartAsync();
            var client = host.GetTestClient();

            //Given
            var gameId = Guid.NewGuid();
            context.Given(new GameCreated { GameId = gameId, PlayerId = "test@tester.com", Rounds = 1, Title = "test game" }
                                .AddTestMetaData<GameState>(new AggregateId(gameId)));

            //When
            await context.WhenAsync(async () =>
            {
                var cmd = new JoinGame { GameId = gameId, PlayerId = "joe@tester.com" };
                var response = await client.PostAsync($"/games/{gameId}/join", new FormUrlEncodedContent(
                      new[] {
                          KeyValuePair.Create(nameof(cmd.GameId), cmd.GameId.ToString()),
                          KeyValuePair.Create(nameof(cmd.PlayerId), cmd.PlayerId),
                      }
                    ), CancellationToken.None);

                Assert.Equal(System.Net.HttpStatusCode.Found, response.StatusCode);
            });

            //Then  
            context.Then((events, visual) =>
            {
                this.helper.WriteLine(visual);
                Assert.True(events.OfType<GameStarted>().Happened());
                Assert.True(events.OfType<RoundStarted>().Happened());
            });
        }
    }
}
