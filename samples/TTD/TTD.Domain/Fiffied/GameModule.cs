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
        public TTDModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher)
            : base(dispatcher, publish, queryDispatcher)
        { }

        public static Module Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<TTDModule>((c, p, q) => new TTDModule(c, p, q))
            .Command<AdvanceTime>(cmd => pub(new IEvent[] { new TimePassed { Time = cmd.Time }
            .Tap(x => x.Meta.Add("correlationid", ((ICommand)cmd).CorrelationId.ToString()))
            .Tap(x => x.Meta.Add("eventid", Guid.NewGuid().ToString()))

            })) //TODO Events.Raise for meta - some trough magic - compose func
            .Command<PlanCargo>(
                Commands.GuaranteeCorrelation<PlanCargo>(),
                cmd => ApplicationService.ExecuteAsync(store, cmd, "all", () => new[] { new CargoPlanned(cmd.CargoId, cmd.Destination, Location.Factory) }, pub))
            .Command<PickUp>(
                Commands.GuaranteeCorrelation<PickUp>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, "all", state => state.Handle(cmd, Route.GetRoutes()), pub))
            .Command<Unload>(
                Commands.GuaranteeCorrelation<Unload>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, "all", state => state.Handle(cmd), pub))
            //.Projection<IEvent>(events => store.AppendToStreamAsync("all", events))
            .Command<ReadyTransport>(
                Commands.GuaranteeCorrelation<ReadyTransport>(),
                cmd => ApplicationService.ExecuteAsync(store, cmd, "all", () => new[] { new TransportReady(cmd.TransportId, cmd.Kind, cmd.Location, cmd.Time) }, pub))
            .Command<Return>(
                Commands.GuaranteeCorrelation<Return>(),
                cmd => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, "all", state => state.Handle(cmd, Route.GetRoutes()), pub))
            .Policy<TimePassed>(Policy.On<TimePassed>(e => GameEngine.When(e, store.GetTransports("all").GetAwaiter().GetResult())))
            .Policy<TransportReady>((e, ctx) => ctx.ExecuteAsync<CargoLocations>("all", p => Policy.Issue(e, () => GameEngine.When(e, p.Locations)))) //TODO execute with array
            .Policy(Policy.On<Arrived>(e => Policy.Issue(e, () => GameEngine.When(e, store.GetTransports("all").GetAwaiter().GetResult()).ToArray()))) //TODO util
            .Query<CargoLocationQuery, CargoLocations>(q => store.Projector<CargoLocations>().ProjectAsync("all"))
            .Create(store);
    }
}

public class CargoLocationQuery : IQuery<CargoLocations>
{ }
