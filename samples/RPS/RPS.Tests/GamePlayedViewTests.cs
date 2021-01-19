using Fiffi;
using System;
using Xunit;

namespace RPS.Tests
{
    public class GamePlayedViewTests
    {
        [Fact]
        public void IntegrationEvent()
        {
            var gamePlayedEvent = GameEvents(Guid.NewGuid(), "test", "alex@rpsgame.com", "lisa@rpsgame.com")
                .Rehydrate<GamePlayed>();

            Assert.Equal("lisa@rpsgame.com", gamePlayedEvent.Winner);
        }

        public static EventRecord[] GameEvents(Guid gameId, string title, string loosingPlayer, string winningPlayer)
            => new EventRecord[] {
                new GameCreated(gameId, loosingPlayer, title, 1, DateTime.UtcNow),
                new GameStarted(gameId, winningPlayer),
                new RoundStarted(gameId, 1),
                new HandShown(gameId, loosingPlayer, Hand.Scissors),
                new HandShown(gameId, winningPlayer, Hand.Rock),
                new RoundEnded(gameId, winningPlayer, loosingPlayer, 1),
                new RoundStarted(gameId, 2),
                new HandShown(gameId, loosingPlayer, Hand.Scissors),
                new HandShown(gameId, winningPlayer, Hand.Paper),
                new RoundEnded(gameId, winningPlayer, loosingPlayer, 2),
                new GameEnded(gameId)
    };

    }
}
