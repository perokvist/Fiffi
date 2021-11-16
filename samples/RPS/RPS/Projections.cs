using Fiffi;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace RPS;

public class GamesView
{
    public Dictionary<string, GameView> Games { get; set; } = new Dictionary<string, GameView>();

    public GamesView When(EventRecord @event) => @event switch
    {
        GameCreated e => When(e),
        GameStarted e => When(e),
        GameEnded e => When(e),
        _ => this
    };

    public GamesView When(GameCreated @event)
    {
        var key = @event.GameId.ToString();
        if (!Games.ContainsKey(key))
        {
            Games.Add(key, new GameView
            (
                Id: @event.GameId,
                Title: @event.Title,
                StartedBy: @event.PlayerId,
                Status: @event.Status.ToString()
            ));
        }
        return this;
    }

    public GamesView When(GameStarted @event)
    {
        var gameId = @event.GameId.ToString();
        Games[gameId] = Games[gameId] with { Status = GameStatus.Started.ToString() };
        return this;
    }

    public GamesView When(GameEnded @event)
    {
        var gameId = @event.GameId.ToString();
        Games[@event.GameId.ToString()] = Games[@event.GameId.ToString()] with { Status = GameStatus.Ended.ToString() };
        //Games.Remove(@event.GameId);
        return this;
    }
}

public record GameView(Guid Id, string Title, string Status, string StartedBy);

public class ScoresView
{
    private Dictionary<Guid, RoundView> scores = new Dictionary<Guid, RoundView>();
    public List<RoundView> Scores => scores.Values.ToList();

    public ScoresView When(EventRecord @event) => @event switch
    {
        GameCreated e => When(e),
        RoundTied e => When(e),
        RoundEnded e => When(e),
        _ => this
    };

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

public record GamePlayed(Guid GameId, int Rounds, string Winner, string Looser) : EventRecord, IIntegrationEvent
{
    public ImmutableDictionary<string, int> g = ImmutableDictionary<string, int>.Empty;

    public GamePlayed() : this(Guid.Empty, 0, string.Empty, string.Empty)
    { }

    public GamePlayed When(EventRecord @event) => Apply(this, @event);

    public static GamePlayed Apply(GamePlayed current, EventRecord @event) => @event switch
    {
        GameCreated e => current with { GameId = e.GameId, Rounds = e.Rounds, g = current.g.Pipe(x => x.Add(e.PlayerId, 0)) },
        GameStarted e => current with { g = current.g.Pipe(x => x.Add(e.PlayerId, 0)) },
        RoundEnded e => current with { g = current.g.Pipe(x => x.SetItem(e.Winner, x[e.Winner] + 1)) },
        GameEnded e => current.g.Pipe(g =>
        {
            var Winner = g
            .OrderByDescending(x => x.Value)
            .First()
            .Key;
            var Loser = g
                .First(x => x.Key != Winner)
                .Key;
            return (Winner, Loser);
        }).Pipe(x => current with { Looser = x.Loser, Winner = x.Winner }),
        _ => current
    };
};

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

