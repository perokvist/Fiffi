using Fiffi;
using System;
using Xunit;
using static Domain;

namespace RPS.Tests
{
    public class GameStateViewTests
    {
        [Fact]
        public void Created()
        {
            var gameId = Guid.NewGuid();

            //Given
            var state = new IEvent[] {
                new GameCreated { GameId = gameId, PlayerId = "test@tester.com", Rounds = 1, Title = "test game" },
            }.Rehydrate<GameState>();

            //Then  
            Assert.Equal(GameState.GameStatus.Created, state.Status);
        }

        [Fact]
        public void Started()
        {
            var gameId = Guid.NewGuid();

            //Given
            var state = new IEvent[] {
                new GameCreated { GameId = gameId, PlayerId = "test@tester.com", Rounds = 1, Title = "test game" },
                new GameStarted(gameId,"foo@tester.com", null),
                new RoundStarted { GameId = gameId, Round = 1 }
            }.Rehydrate<GameState>();

            //Then  
            Assert.Equal(GameState.GameStatus.Started, state.Status);
        }

        [Fact]
        public void Ended()
        {
            var gameId = Guid.NewGuid();

            //Given
            var state = new IEvent[] {
                new GameCreated { GameId = gameId, PlayerId = "lisa@tester.com", Rounds = 1, Title = "test game" },
                new GameStarted(gameId, "alex@tester.com", null),
                new RoundStarted { GameId = gameId, Round = 1 },
                new HandShown { GameId = gameId, Hand = Hand.Paper, PlayerId = "lisa@tester.com" },
                new HandShown { GameId = gameId, Hand = Hand.Rock, PlayerId = "alex@tester.com" },
                new RoundEnded { GameId = gameId, Round = 1, Looser = "lisa@tester.com", Winner = "alex@tester.com" },
                new GameEnded(gameId, null)
            }.Rehydrate<GameState>();

            //Then  
            Assert.Equal(GameState.GameStatus.Ended, state.Status);
        }
    }
}
