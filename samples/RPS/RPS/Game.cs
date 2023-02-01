using Fiffi;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static RPS.GameState;
using static RPS.State;

namespace RPS;

public static class Game
{
    public static EventRecord[] Handle<T>(T command, GameState state)
         where T : ICommand
        => command switch
        {
            CreateGame c => Handle(c, state).ToArray(),
            JoinGame c => Handle(c, state).ToArray(),
            PlayGame c => Handle(c, state).ToArray(),
            _ => throw new InvalidOperationException($"No handler for {command.GetType()} found.")
        };

    public static IEnumerable<EventRecord> Handle(CreateGame command, GameState state)
     => new[] {
             new GameCreated(GameId : command.GameId,
                             PlayerId : command.PlayerId,
                             Title : command.Title,
                             Rounds : command.Rounds,
                             Created : DateTime.UtcNow,
                             Status : GameStatus.ReadyToStart)
     };

    public static IEnumerable<EventRecord> Handle(JoinGame command, GameState state)
    {
        if (state.Players.PlayerOne.Id == command.PlayerId)
            yield break;

        if (state.Players.PlayerTwo.Hand == Hand.None)
        {
            yield return new GameStarted(GameId: command.GameId, PlayerId: command.PlayerId);
            yield return new RoundStarted(GameId: command.GameId, Round: 1);
        }
    }

    public static IEnumerable<EventRecord> Handle(PlayGame command, GameState state)
    {
        if (state.Status != GameStatus.Started)
            yield break;

        if (command.Hand == Hand.None) //TODO validation
            yield break;

        var players = state.Players switch
        {
            { } p when command.PlayerId == p.PlayerOne.Id => (Active: state.Players.PlayerOne, Passive: state.Players.PlayerTwo), { } p when command.PlayerId == p.Item1.Id => (Active: state.Players.Item1, Passive: state.Players.Item2),
            { } p when command.PlayerId == p.PlayerTwo.Id => (Active: state.Players.PlayerTwo, Passive: state.Players.PlayerOne),
            _ => throw new ArgumentException("Player not in game")
        };

        yield return players.Active.Hand switch
        {
            Hand.None => new HandShown(GameId: command.GameId, PlayerId: players.Active.Id, Hand: command.Hand),
            _ => throw new ArgumentException("Changing hand not allowed")
        };

        var endRound = players.Passive.Hand != Hand.None;

        if (!endRound)
            yield break;


        var activePlayerResult = (command.Hand, players.Passive.Hand) switch
        {
            (Hand.Paper, Hand.Paper) => RoundResult.Tied,
            (Hand.Paper, Hand.Rock) => RoundResult.Won,
            (Hand.Paper, Hand.Scissors) => RoundResult.Lost,
            (Hand.Rock, Hand.Paper) => RoundResult.Lost,
            (Hand.Rock, Hand.Rock) => RoundResult.Tied,
            (Hand.Rock, Hand.Scissors) => RoundResult.Won,
            (Hand.Scissors, Hand.Paper) => RoundResult.Won,
            (Hand.Scissors, Hand.Rock) => RoundResult.Lost,
            (Hand.Scissors, Hand.Scissors) => RoundResult.Tied,
            _ => RoundResult.Tied
        };

        yield return activePlayerResult switch
        {
            RoundResult.Won => new RoundEnded(GameId: command.GameId, Winner: players.Active.Id, Looser: players.Passive.Id, Round: state.Round),
            RoundResult.Lost => new RoundEnded(GameId: command.GameId, Winner: players.Passive.Id, Looser: players.Active.Id, Round: state.Round),
            _ => new RoundTied(GameId: command.GameId, Round: state.Round),
        };

        yield return (state.Rounds == state.Round) switch
        {
            true => new GameEnded(GameId: command.GameId),
            _ => new RoundStarted(GameId: command.GameId, Round: state.Round + 1)
        };
    }
}

public record GameCreated(
         Guid GameId,
        string PlayerId,
        string Title,
        int Rounds,
        DateTime Created,
        GameStatus Status = GameStatus.Started
    ) : EventRecord;

public record RoundStarted(Guid GameId, int Round) : EventRecord;
public record GameStarted(Guid GameId, string PlayerId) : EventRecord;
public record GameEnded(Guid GameId) : EventRecord;
public record RoundTied(Guid GameId, int Round) : EventRecord;
public record RoundEnded(Guid GameId, string Winner, string Looser, int Round) : EventRecord;
public record HandShown(Guid GameId, string PlayerId, Hand Hand) : EventRecord;

public enum Hand
{
    None = 0,
    Rock = 10,
    Paper = 20,
    Scissors = 30
}

public enum RoundResult
{
    Tied = 10,
    Won = 20,
    Lost = 30
}

public class PlayGame : IGameCommand
{
    [ScaffoldColumn(false)]
    public Guid GameId { get; set; }
    public Hand Hand { get; set; }
    public string PlayerId { get; set; }

    IAggregateId ICommand.AggregateId => new AggregateId(GameId);

    Guid ICommand.CorrelationId { get; set; }
    Guid ICommand.CausationId { get; set; }
}

public class JoinGame : IGameCommand
{
    [ScaffoldColumn(false)]
    public Guid GameId { get; set; }

    public string PlayerId { get; set; }

    IAggregateId ICommand.AggregateId => new AggregateId(GameId);
    Guid ICommand.CorrelationId { get; set; }
    Guid ICommand.CausationId { get; set; }
}


public class CreateGame : IGameCommand
{
    [Required]
    [ScaffoldColumn(false)]
    public Guid GameId { get; set; }

    [Required]
    public string PlayerId { get; set; }
    [Required]
    public string Title { get; set; }

    [Required]
    public int Rounds { get; set; }
    IAggregateId ICommand.AggregateId => new AggregateId(GameId);
    Guid ICommand.CorrelationId { get; set; }
    Guid ICommand.CausationId { get; set; }
}

public static class State
{
    public static GameState Evolve(GameState state, EventRecord @event)
     => @event switch
     {
         GameCreated e => state with
         {
             Status = GameStatus.ReadyToStart,
             Id = e.GameId,
             Players = (new(e.PlayerId, default), new(string.Empty, default)),
             Rounds = e.Rounds
         },
         GameStarted e => state with
         {
             Status = GameStatus.Started,
             Players = (state.Players.PlayerOne, new(e.PlayerId, default))
         },
         RoundStarted e => state with
         {
             Status = GameStatus.Started,
             Round = e.Round
         },
         HandShown e => state with
         {
             Players = e.PlayerId switch
             {
                 string id when id == state.Players.PlayerOne.Id => new(state.Players.PlayerOne with { Hand = e.Hand }, state.Players.PlayerTwo),
                 _ => new(state.Players.PlayerOne, state.Players.PlayerTwo with { Hand = e.Hand })
             }
         },
         RoundTied => state with
         {
             Players = (state.Players.PlayerOne with { Hand = Hand.None }, state.Players.PlayerTwo with { Hand = Hand.None })
             //TODO set all hand and status trough events
         },
         GameEnded => state with
         {
             Status = GameStatus.Ended
         },
         _ => state
     };
}

public record GameState(
 Guid Id,
 (Player PlayerOne, Player PlayerTwo) Players,
 int Round,
 int Rounds,
 GameStatus Status,
 long Version)
{
    public GameState() : this(Guid.NewGuid(), (default, default), 0, 0, GameStatus.None, 0)
    { }

    public GameState When(EventRecord @event) => State.Evolve(this, @event);
}

public record Player(string Id, Hand Hand);

public enum GameStatus
{
    None = 0,
    ReadyToStart = 10,
    Started = 20,
    Ended = 50
}
public interface IGameCommand : ICommand
{ }
