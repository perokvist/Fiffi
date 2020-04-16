using Fiffi;
using Fiffi.Modularization;
using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RPS
{
    public class GameModule : Module
    {
        public GameModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher) 
            : base(dispatcher, publish, queryDispatcher)
        {}

        public static GameModule Initialize(IEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<GameModule>((c, p, q) => new GameModule(c, p, q))
            .Command<IGameCommand>(
                Commands.GuaranteeCorrelation<IGameCommand>(),
                cmd => ApplicationService.ExecuteAsync<GameState>(store, cmd, state => Game.Handle(cmd, state), pub))
            .Projection<GameStarted>(e => store.AppendToStreamAsync(Streams.Games, e))
            .Projection<GameEnded>(e => store.AppendToStreamAsync(Streams.Games, e))
            .Projection<IEvent>(e => store.AppendToStreamAsync(Streams.All, e))
            .Projection<GameEnded>(e => store.Projector<GamePlayed>().Publish(Streams.All, pub)) //TODO meta data
            //.Policy<IEvent>((e, ctx) => ctx.ExecuteAsync()
            .Query<GamesQuery, GamesView>(q => store.Projector<GamesView>().ProjectAsync(Streams.Games)) //TODO ext with stream name only
            .Create(store);
    }

    public static class Streams
    {
        public const string Games = "Games";
        public const string All = "All";
    }

    public class GamesView
    {
        public Dictionary<Guid, (string Title, bool Started)> Games { get; set; } = new Dictionary<Guid, (string, bool)>();

        public GamesView When(IEvent @event) => this;

        public GamesView When(GameCreated @event)
        {
            Games.Add(@event.GameId, (@event.Title, false));
            return this;
        }

        public GamesView When(GameStarted @event)
        {
            var game = Games[@event.GameId];
            Games[@event.GameId] = (game.Title, true);
            return this;
        }

        public GamesView When(GameEnded @event)
        {
            Games.Remove(@event.GameId);
            return this;
        }

    }

    public class GamePlayed : IEvent, IIntegrationEvent
    {
        public Guid GameId { get; set; }
        string IEvent.SourceId => GameId.ToString();
        IDictionary<string, string> IEvent.Meta { get; set; }
    }

    public class GamesQuery : IQuery<GamesView>
    {
    }
}
