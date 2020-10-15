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
        public TTDModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
            : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static Module Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<TTDModule>((c, p, q, s) => new TTDModule(c, p, q, s))
            .Command<AdvanceTime>(cmd => ApplicationService.ExecuteAsync(cmd, () => new IEvent[] { new TimePassed { Time = cmd.Time } }, pub))
            .Command<PlanCargo>(
                Commands.GuaranteeCorrelation<PlanCargo>(),
                cmd => ApplicationService.ExecuteAsync(store, cmd, Streams.All, () => new[] { new CargoPlanned(cmd.CargoId, cmd.Destination, Location.Factory) }, pub))
            .Command<PickUp>(
                Commands.GuaranteeCorrelation<PickUp>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, Streams.All, state => state.Handle(cmd, Route.GetRoutes()), pub))
            .Command<Unload>(
                Commands.GuaranteeCorrelation<Unload>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, Streams.All, state => state.Handle(cmd), pub))
            .Command<ReadyTransport>(
                Commands.GuaranteeCorrelation<ReadyTransport>(),
                cmd => ApplicationService.ExecuteAsync(store, cmd, Streams.All, () => new[] { new TransportReady(cmd.TransportId, cmd.Kind, cmd.Location, cmd.Time) }, pub))
            .Command<Return>(
                Commands.GuaranteeCorrelation<Return>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, Streams.All, state => state.Handle(cmd, Route.GetRoutes()), pub))
            .Policy(Policy.On<TimePassed, Transport, ITransportEvent>(Streams.All, GameEngine.When))
            .Policy(Policy.On<TransportReady, CargoLocations>(Streams.All, (e, p) => GameEngine.When(e, p.Locations)))
            .Policy(Policy.On<Arrived, Transport, ITransportEvent>(Streams.All, GameEngine.When))
            .Query<CargoLocationQuery, CargoLocations>(q => store.Projector<CargoLocations>().ProjectAsync(Streams.All))
            .Create(store);
    }
}

public class Streams
{
    public const string All = "all";
}

public class CargoLocationQuery : IQuery<CargoLocations>
{ }
