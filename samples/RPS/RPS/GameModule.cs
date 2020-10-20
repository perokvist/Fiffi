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
        public GameModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher, Func<IEvent[], Task> onStart)
            : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static GameModule Initialize(
            IAdvancedEventStore store,
            ISnapshotStore snapshotStore,
            Func<IEvent[], Task> pub)
            => new ModuleConfiguration<GameModule>((c, p, q, s) => new GameModule(c, p, q, s))
            .Command<IGameCommand>(
                Commands.Validate<IGameCommand>(),
                Commands.GuaranteeCorrelation<IGameCommand>(),
                cmd => store.ExecuteAsync<GameState>(cmd, state =>
                    Game.Handle(cmd, state).ToEnvelopes(cmd.AggregateId.Id),
                    events =>
                    {
                        //await snapshotStore.Apply<GamesView>(events.Apply);
                        //await pub(events);
                        return Task.CompletedTask;
                    }, snapshotStore, state => state.Version, (newVersion, state) => state.Pipe(x => x with { Version = newVersion })))
            .Projection<IEvent>(async events =>
            {
                await store.AppendToStreamAsync(Streams.Games, events.Filter(typeof(GameCreated), typeof(GameStarted), typeof(GameEnded)));
                await store.Projector<GamesView>().Project(Streams.Games, snapshotStore);
            })
            .Projection<IEvent>(events => store.AppendToStreamAsync(Streams.All, events.ToArray()))
            //.Policy<GameEnded>((e, ctx) => ctx.Store.Projector<GamePlayed>().Publish(e, Streams.All, pub))
            .Query<GamesQuery, GamesView>(q => snapshotStore.Get<GamesView>())
            .Query<GameQuery, GameView>(async q => (await store.Projector<GamesView>().ProjectAsync(Streams.Games)).Games.First(x => x.Key == q.GameId.ToString()).Value) //TODO ext with stream name only
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
        IDictionary<string, string> IEvent.Meta { get; set; } = new Dictionary<string, string>();

        public GamePlayed When(IEvent @event) => this;
    }
}
