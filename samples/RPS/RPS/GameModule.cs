using Fiffi;
using Fiffi.Modularization;
using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace RPS
{
    public class GameModule : Module
    {
        public GameModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher, Func<IEvent[], Task> onStart)
            : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static GameModule Initialize(
            IAdvancedEventStore store,
            ISnapshotStore snapshotStore,
            Func<IEvent[], Task> pub)
                => new Configuration<GameModule>((c, p, q, s) => new GameModule(c, p, q, s))
            .Commands(
                Commands.Validate<ICommand>(),
                Commands.GuaranteeCorrelation<ICommand>(),
                cmd => store.ExecuteAsync<GameState>(cmd, state =>
                    Game.Handle(cmd, state),
                    async events =>
                    {
                        await snapshotStore.Apply<GamesView>(events.Select(e => e.Event).Apply);
                        await pub(events);
                    }, snapshotStore, state => state.Version, (newVersion, state) => state.Pipe(x => x with { Version = newVersion })))
            .Updates(async events =>
            {
                await store.AppendToStreamAsync(Streams.Games, events.Filter(typeof(GameCreated), typeof(GameStarted), typeof(GameEnded)));
                await store.Projector<GamesView>().Project(Streams.Games, snapshotStore);
            })
            .Updates(events => store.AppendToStreamAsync(Streams.All, events.ToArray()))
            .Triggers(async (events, d) =>
            {
                await foreach (var t in events.Select(e => e switch
                  {
                      GameEnded evt => store.Projector<GamePlayed>().Publish(e, Streams.All, pub),
                      _ => Task.CompletedTask
                  }).ToAsyncEnumerable()) ;
            })
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

    public record GamePlayed(Guid GameId, int Rounds, string Winner, string Looser) : EventRecord, IIntegrationEvent
    {
        public ImmutableDictionary<string, int> g = ImmutableDictionary<string, int>.Empty;

        public GamePlayed() : this(Guid.Empty, 0, string.Empty, string.Empty)
        {}

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
                var Looser = g
                    .First(x => x.Key != Winner)
                    .Key;
                return (Winner, Looser);
            }).Pipe(x => current with { Looser = x.Looser, Winner = x.Winner }),
            _ => current
        };
    };
}