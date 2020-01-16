using Fiffi;
using Fiffi.Modularization;
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
            .Command(
                Commands.GuaranteeCorrelation<CreateGame>(),
                cmd => ApplicationService.ExecuteAsync<GameState>(store, cmd, state => Game.Handle(cmd, state).ToArray(), pub))
            .Create(store);

    }
}
