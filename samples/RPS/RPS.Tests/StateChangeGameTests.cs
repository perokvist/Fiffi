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
    public class StateChangeGameTests
    {
        ITestContext context;
        ITestOutputHelper helper;

        public StateChangeGameTests(ITestOutputHelper outputHelper)
         => context = TestContextBuilder.Create<InMemoryEventStore, GameModule>((store, pub) =>
            {
                this.helper = outputHelper;
                return GameModule.Initialize(store, pub);
            });

        [Fact]
        public async Task CreateGame()
        {
            //Given
            context.Given(Array.Empty<IEvent>());

            //When
            await context.WhenAsync(new CreateGame { Rounds = 3, GameId = Guid.NewGuid(), PlayerId = "tester", Title = "Test Game" });

            //Then  
            context.Then((events, visual) => {
                this.helper.WriteLine(visual);
                Assert.True(events.OfType<GameCreated>().Happened());
            });
        }

        [Fact]
        public async Task JoinGame()
        {
            var gameId = Guid.NewGuid();

            //Given
            context.Given(new GameCreated { GameId = gameId, PlayerId = "test@tester.com", Rounds = 1, Title = "test game"  }
                                .AddTestMetaData<GameState>(new AggregateId(gameId))); 

            //When
            await context.WhenAsync(new JoinGame { GameId = gameId, PlayerId = "test2@tester.com" });

            //Then  
            context.Then((events, visual) => {
                this.helper.WriteLine(visual);
                Assert.True(events.OfType<GameStarted>().Happened());
                Assert.True(events.OfType<RoundStarted>().Happened());
            });
        }

  

        [Fact]
        public async Task Handshown()
        {
            var gameId = Guid.NewGuid();

            //Given
            context.Given<GameState>(new AggregateId(gameId), 
                new GameCreated { GameId = gameId, PlayerId = "test@tester.com", Rounds = 1, Title = "test game" },
                new GameStarted { GameId = gameId, PlayerId = "foo@tester.com" },
                new RoundStarted { GameId = gameId, Round = 1 });

            //When
            await context.WhenAsync(new PlayGame { GameId = gameId, PlayerId = "foo@tester.com", Hand = Hand.Paper });

            //Then  
            context.Then((events, visual) => {
                this.helper.WriteLine(visual);
                Assert.True(events.OfType<HandShown>().Happened());
            });
        }

        [Fact]
        public async Task GameEnd()
        {
            var gameId = Guid.NewGuid();

            //Given
            context.Given<GameState>(new AggregateId(gameId),
                new GameCreated { GameId = gameId, PlayerId = "test@tester.com", Rounds = 1, Title = "test game" },
                new GameStarted { GameId = gameId, PlayerId = "foo@tester.com" },
                new RoundStarted { GameId = gameId, Round = 1 },
                new HandShown { GameId = gameId, PlayerId = "test@tester.com", Hand = Hand.Paper }
                );

            //When
            await context.WhenAsync(new PlayGame { GameId = gameId, PlayerId = "foo@tester.com", Hand = Hand.Rock });

            //Then  
            context.Then((events, visual) => {
                this.helper.WriteLine(visual);
                Assert.True(events.OfType<GameEnded>().Happened());
            });
        }
    }
}
