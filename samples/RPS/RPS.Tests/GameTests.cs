using Fiffi;
using Fiffi.Testing;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RPS.Tests
{
    public class GameTests
    {
        ITestContext context;
        public GameTests()
         => context = TestContextBuilder.Create<InMemoryEventStore, GameModule>((store, pub) =>
            {
                //this.helper = outputHelper;
                var module = GameModule.Initialize(store, pub);
                return module;
            });

        [Fact]
        public async Task CreateGame()
        {
            //When
            await context.WhenAsync(new CreateGame { FirstTo = 3, GameId = Guid.NewGuid(), PlayerId = "tester", Title = "Test Game" });

            //Then
            context.Then(events => Assert.True(events.OfType<GameCreated>().Happened()));

        }
    }
}
