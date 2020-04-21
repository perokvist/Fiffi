﻿using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RPS
{
    public class GamesView
    {
        public Dictionary<Guid, GameView> Games = new Dictionary<Guid, GameView>();

        public GamesView When(IEvent @event) => this;

        public GamesView When(GameCreated @event)
        {
            Games.Add(@event.GameId, new GameView
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
            var game = Games[@event.GameId].Status = GameState.GameStatus.Started.ToString();
            return this;
        }

        public GamesView When(GameEnded @event)
        {
            var game = Games[@event.GameId].Status = GameState.GameStatus.Ended.ToString();
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

        public ScoresView When(RoundEnded @event)
        {
            scores[@event.GameId] = new RoundView
            {
                Id = @event.GameId,
                Looser = @event.Looser,
                Winner = @event.Winner,
                Rounds = @event.Round
            };
            return this;
        }
    }

    public class RoundView
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
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

