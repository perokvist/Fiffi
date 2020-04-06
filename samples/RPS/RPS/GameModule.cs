using Fiffi;
using Fiffi.Modularization;
using Fiffi.Projections;
using System;
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
                cmd => ApplicationService.ExecuteAsync<GameState>(store, cmd, state => Task.FromResult(Game.Handle(cmd, state).ToArray()), pub))
            .Projection<GameStarted>(e => store.AppendToStreamAsync(Streams.Games, e))
            //TODO "all" stream
            .Query<GamesQuery, GamesView>(q => store.Projector<GamesView>().ProjectAsync(Streams.Games)) //TODO ext with stream name only
            .Create(store);
    }

    public static class Streams
    {
        public const string Games = "Games";
    }

    internal class GamesView
    {
    }

    internal class GamesQuery : IQuery<GamesView>
    {
    }
}
