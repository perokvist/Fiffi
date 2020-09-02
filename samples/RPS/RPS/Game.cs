using Fiffi;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static RPS.GameState;

namespace RPS
{
    public static class Game
    {
        public static IEvent[] Handle<T>(T command, GameState state)
             where T : IGameCommand
            => ((IEnumerable<IEvent>)Handle((dynamic)command, state)).ToArray();

        public static IEvent[] Handle(object command, GameState state) => throw new InvalidOperationException($"No handler for {command.GetType()} found.");


        public static IEnumerable<IEvent> Handle(CreateGame command, GameState state)
         => new[] {
                    new GameCreated
                    {
                               GameId = command.GameId,
                               PlayerId = command.PlayerId,
                               Title = command.Title,
                               Rounds = command.Rounds,
                               Created = DateTime.UtcNow,
                               Status = GameStatus.ReadyToStart
                    }
                  };

        public static IEnumerable<IEvent> Handle(JoinGame command, GameState state)
        {
            if (state.Players.PlayerOne.Id == command.PlayerId)
                yield break;

            if (state.Players.PlayerTwo == default)
            {
                yield return new GameStarted { GameId = command.GameId, PlayerId = command.PlayerId };
                yield return new RoundStarted { GameId = command.GameId, Round = 1 };
            }
        }

        public static IEnumerable<IEvent> Handle(PlayGame command, GameState state)
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
                Hand.None => new HandShown { GameId = command.GameId, PlayerId = players.Active.Id, Hand = command.Hand },
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
                RoundResult.Won => new RoundEnded { GameId = command.GameId, Winner = players.Active.Id, Looser = players.Passive.Id, Round = state.Round },
                RoundResult.Lost => new RoundEnded { GameId = command.GameId, Winner = players.Passive.Id, Looser = players.Active.Id, Round = state.Round },
                _ => new RoundTied { GameId = command.GameId, Round = state.Round },
            };

            yield return (state.Rounds == state.Round) switch
            {
                true => new GameEnded { GameId = command.GameId },
                _ => new RoundStarted { GameId = command.GameId, Round = state.Round + 1 }
            };
        }

    }

    public class GameCreated : IEvent
    {
        public Guid GameId { get; set; }
        public string PlayerId { get; set; }
        public string Title { get; set; }
        public int Rounds { get; set; }
        public DateTime Created { get; set; }
        public GameStatus Status { get; set; } = GameStatus.Started;
        public string SourceId => GameId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }

    public class RoundStarted : IEvent
    {
        public Guid GameId { get; set; }

        public int Round { get; set; }

        public string SourceId => GameId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }

    public class GameStarted : IEvent
    {
        public Guid GameId { get; set; }

        public string PlayerId { get; set; }

        public string SourceId => GameId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }

    public class GameEnded : IEvent
    {
        public Guid GameId { get; set; }

        public string SourceId => GameId.ToString();

        public IDictionary<string, string> Meta { get; set; }
    }

    public class RoundTied : IEvent
    {
        public Guid GameId { get; set; }
        public int Round { get; set; }
        public string SourceId => GameId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }

    public class RoundEnded : IEvent
    {
        public Guid GameId { get; set; }
        public string Winner { get; set; }
        public string Looser { get; set; }
        public int Round { get; set; }
        public string SourceId => GameId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }

    public class HandShown : IEvent
    {
        public Guid GameId { get; set; }
        public string PlayerId { get; set; }
        public Hand Hand { get; set; }

        public string SourceId => GameId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }

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

    public class GameState
    {
        public Guid Id { get; set; }
        public (Player PlayerOne, Player PlayerTwo) Players { get; set; }
        public int Round { get; set; }
        public int Rounds { get; set; }
        public GameStatus Status { get; set; }
        public long Version { get; set; }

        public GameState When(IEvent @event) => this;

        public GameState When(GameCreated @event)
        {
            Status = GameStatus.ReadyToStart;
            Id = @event.GameId;
            Players = (new Player { Id = @event.PlayerId }, default);
            Rounds = @event.Rounds;
            return this;
        }

        public GameState When(GameStarted @event)
        {
            Status = GameStatus.Started;
            Players = (Players.PlayerOne, new Player { Id = @event.PlayerId });
            return this;
        }

        public GameState When(RoundStarted @event)
        {
            Status = GameStatus.Started;
            Round = @event.Round;
            return this;
        }

        public GameState When(HandShown @event)
        {
            var p = @event.PlayerId switch
            {
                string id when id == Players.PlayerOne.Id => Players.PlayerOne,
                _ => Players.PlayerTwo
            };
            p.Hand = @event.Hand;

            return this;
        }

        public GameState When(RoundTied @event)
        {
            Players.PlayerOne.Hand = Hand.None; //TODO set all hand and status trough events
            Players.PlayerTwo.Hand = Hand.None;
            return this;
        }

        public GameState When(GameEnded @event)
        {
            Status = GameStatus.Ended;
            return this;
        }

        public class Player
        {
            public string Id { get; set; }
            public Hand Hand { get; set; }
        }

        public enum GameStatus
        {
            None = 0,
            ReadyToStart = 10,
            Started = 20,
            Ended = 50
        }
    }

    public interface IGameCommand : ICommand
    { }
}
