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

        public static GameModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<GameModule>((c, p, q) => new GameModule(c, p, q))
            .Command<IGameCommand>(
                Commands.Validate<IGameCommand>(),
                Commands.GuaranteeCorrelation<IGameCommand>(),
                cmd => store.ExecuteAsync<GameState>(cmd, state => Game.Handle(cmd, state), pub))
            .Projection<GameCreated>(e => store.AppendToStreamAsync(Streams.Games, e))
            .Projection<GameStarted>(e => store.AppendToStreamAsync(Streams.Games, e))
            .Projection<GameEnded>(e => store.AppendToStreamAsync(Streams.Games, e))
            .ProjectionBatch<IEvent>(e => store.AppendToStreamAsync(Streams.All, e))
            //.Projection<GameEnded>(e => store.Projector<GamePlayed>().Publish(Streams.All, pub)) //TODO meta data
            .Query<GamesQuery, GamesView>(q => store.Projector<GamesView>().ProjectAsync(Streams.Games)) //TODO ext with stream name only
            .Query<GameQuery, GameView>(async q => (await store.Projector<GamesView>().ProjectAsync(Streams.Games)).Games.First(x => x.Key == q.GameId).Value) //TODO ext with stream name only
            .Query<ScoreQuery, ScoresView>(q => store.Projector<ScoresView>().ProjectAsync(Streams.All))
            .Create(store);
    }

    public static class Streams
    {
        public const string Games = "Games";
        public const string All = "All";
    }
    public class GamePlayed : IEvent, IIntegrationEvent
    {
        public Guid GameId { get; set; }
        string IEvent.SourceId => GameId.ToString();
        IDictionary<string, string> IEvent.Meta { get; set; }

        public GamePlayed When(IEvent @event) => this;
    }
}
