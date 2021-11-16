using Fiffi;
using System;
using Xunit;

namespace RPS.Tests;

public class GameStateViewTests
{
    [Fact]
    public void Created()
    {
        var gameId = Guid.NewGuid();

        //Given
        var state = new EventRecord[] {
                new GameCreated( GameId : gameId, PlayerId : "test@tester.com", Rounds : 1, Title : "test game", Created : DateTime.UtcNow),
            }.Rehydrate<GameState>();

        //Then  
        Assert.Equal(GameStatus.ReadyToStart, state.Status);
    }

    [Fact]
    public void Started()
    {
        var gameId = Guid.NewGuid();

        //Given
        var state = new EventRecord[] {
                new GameCreated(GameId : gameId, PlayerId : "test@tester.com", Rounds : 1, Title : "test game", Created : DateTime.UtcNow),
                new GameStarted(GameId : gameId, PlayerId : "foo@tester.com"),
                new RoundStarted(GameId : gameId, Round : 1)
            }.Rehydrate<GameState>();

        //Then  
        Assert.Equal(GameStatus.Started, state.Status);
    }

    [Fact]
    public void Ended()
    {
        var gameId = Guid.NewGuid();

        //Given
        var state = new EventRecord[] {
                new GameCreated(GameId : gameId, PlayerId : "lisa@tester.com", Rounds : 1, Title : "test game", Created : DateTime.UtcNow),
                new GameStarted(GameId : gameId, PlayerId : "alex@tester.com"),
                new RoundStarted(GameId : gameId, Round : 1),
                new HandShown(GameId : gameId, Hand : Hand.Paper, PlayerId : "lisa@tester.com"),
                new HandShown(GameId : gameId, Hand : Hand.Rock, PlayerId : "alex@tester.com"),
                new RoundEnded(GameId : gameId, Round : 1, Looser : "lisa@tester.com", Winner : "alex@tester.com"),
                new GameEnded(GameId : gameId)
            }.Rehydrate<GameState>();

        //Then  
        Assert.Equal(GameStatus.Ended, state.Status);
    }
}
