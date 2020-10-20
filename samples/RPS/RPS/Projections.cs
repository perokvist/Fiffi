using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPS
{
    public class GamesView
    {
        public Dictionary<string, GameView> Games { get; set; } = new Dictionary<string, GameView>();

        public GamesView When(IEvent @event) => this;

        public GamesView When(GameCreated @event)
        {
            Games.Add(@event.GameId.ToString(), new GameView
            {
                Id = @event.GameId,
                Title = @event.Title,
                StartedBy = @event.PlayerId,
                Status = @event.Status.ToString()
            });
            return this;
        }

        public GamesView When(GameStarted @event)
        {
            var gameId = @event.GameId.ToString();
            Games[gameId].Status = GameStatus.Started.ToString();
            return this;
        }

        public GamesView When(GameEnded @event)
        {
            var gameId = @event.GameId.ToString();
            Games[@event.GameId.ToString()].Status = GameStatus.Ended.ToString();
            //Games.Remove(@event.GameId);
            return this;
        }
    }

    public class GameView
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Status { get; set; }
        public string StartedBy { get; set; }
    }

    public class ScoresView
    {
        private Dictionary<Guid, RoundView> scores = new Dictionary<Guid, RoundView>();
        public List<RoundView> Scores => scores.Values.ToList();

        public ScoresView When(IEvent @event) => this;

        public ScoresView When(GameCreated @event)
        {
            scores[@event.GameId] = new RoundView
            {
                Id = @event.GameId,
                Title = @event.Title,
                Rounds = @event.Rounds,
                Host = @event.PlayerId
            };
            return this;
        }

        public ScoresView When(RoundTied @event)
        {
            scores[@event.GameId].Looser = "tied";
            scores[@event.GameId].Winner = "tied";
            return this;
        }

        public ScoresView When(RoundEnded @event)
        {
            scores[@event.GameId].Looser = @event.Looser;
            scores[@event.GameId].Winner = @event.Winner;
            return this;
        }
    }

    public class RoundView
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Host { get; set; }
        public string Looser { get; set; }
        public string Winner { get; set; }
        public int Rounds { get; set; }
        public bool Ongoing { get; set; }
    }

    public class GamesQuery : IQuery<GamesView>
    {
    }

    public class GameQuery : IQuery<GameView>
    {
        public Guid GameId { get; set; }
    }

    public class ScoreQuery : IQuery<ScoresView>
    {
    }
}

