using Fiffi;
using Fiffi.Modularization;
using System;
using System.Threading.Tasks;
using Fiffi.Projections;
using System.Linq;
using TTD;

namespace TTD.Fiffied
{
    public class TTDModule : Module
    {
        public TTDModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
            : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static Module Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<TTDModule>((c, p, q, s) => new TTDModule(c, p, q, s))
            .Command<AdvanceTime>(cmd => ApplicationService.ExecuteAsync(cmd, () => new IEvent[] { new TimePassed { Time = cmd.Time } }, pub))
            .Command<PlanCargo>(
                Commands.GuaranteeCorrelation<PlanCargo>(),
                cmd => ApplicationService.ExecuteAsync(store, cmd, "all", () => new[] { new CargoPlanned(cmd.CargoId, cmd.Destination, Location.Factory) }, pub))
            .Command<PickUp>(
                Commands.GuaranteeCorrelation<PickUp>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, "all", state => state.Handle(cmd, Route.GetRoutes()), pub))
            .Command<Unload>(
                Commands.GuaranteeCorrelation<Unload>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, "all", state => state.Handle(cmd), pub))
            .Command<ReadyTransport>(
                Commands.GuaranteeCorrelation<ReadyTransport>(),
                cmd => ApplicationService.ExecuteAsync(store, cmd, "all", () => new[] { new TransportReady(cmd.TransportId, cmd.Kind, cmd.Location, cmd.Time) }, pub))
            .Command<Return>(
                Commands.GuaranteeCorrelation<Return>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, "all", state => state.Handle(cmd, Route.GetRoutes()), pub))
            .Policy<TimePassed>((e, ctx) => ctx.ExecuteAsync<Transport, ITransportEvent>("all", p => GameEngine.When(e, p)))
            .Policy<TransportReady>((e, ctx) => ctx.ExecuteAsync<CargoLocations>("all", p => Policy.Issue(e, () => GameEngine.When(e, p.Locations))))
            .Policy(Policy.On<Arrived, Transport, ITransportEvent>("all", (e, p) => Policy.Issue(e, () => GameEngine.When(e, p).ToArray())))
            .Query<CargoLocationQuery, CargoLocations>(q => store.Projector<CargoLocations>().ProjectAsync("all"))
            .Create(store);
    }
}

public class CargoLocationQuery : IQuery<CargoLocations>
{ }
