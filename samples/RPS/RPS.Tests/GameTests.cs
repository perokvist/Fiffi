using Fiffi;
using Fiffi.Testing;
using Fiffi.Visualization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace RPS.Tests
{
    public class GameTests
    {
        ITestContext context;
        ITestOutputHelper helper;

        public GameTests(ITestOutputHelper outputHelper)
         => context = TestContextBuilder.Create<InMemoryEventStore, GameModule>((store, pub) =>
            {
                this.helper = outputHelper;
                var module = GameModule.Initialize(store, pub);
                return module;
            });

        [Fact]
        public async Task CreateGame()
        {
            //Given
            context.Given(Array.Empty<IEvent>());

            //When
            await context.WhenAsync(new CreateGame { FirstTo = 3, GameId = Guid.NewGuid(), PlayerId = "tester", Title = "Test Game" });

            //Then  
            context.Then((events, visual) => {
                this.helper.WriteLine(visual);
                Assert.True(events.OfType<GameCreated>().Happened());
            });
        }
    }
}
