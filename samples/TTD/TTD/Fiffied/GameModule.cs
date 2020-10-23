using Fiffi;
using Fiffi.Modularization;
using System;
using System.Threading.Tasks;
using Fiffi.Projections;
using System.Linq;
using TTD;
using System.Security.Cryptography.X509Certificates;

namespace TTD.Fiffied
{
    public class TTDModule : Module
    {
        public TTDModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
            : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static Module Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new Configuration<TTDModule>((c, p, q, s) => new TTDModule(c, p, q, s))
            .Commands(Commands.GuaranteeCorrelation<ICommand>(),
                cmd => cmd switch
                {
                    AdvanceTime c => ApplicationService.ExecuteAsync(cmd, () => new EventRecord[] { new TimePassed { Time = c.Time } }, pub),
                    PlanCargo c => ApplicationService.ExecuteAsync(store, cmd, Streams.All, () => new[] { new CargoPlanned(c.CargoId, c.Destination, Location.Factory) }, pub),
                    PickUp c => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, Streams.All, state => state.Handle(c, Route.GetRoutes()), pub),
                    Unload c => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, Streams.All, state => state.Handle(c), pub),
                    ReadyTransport c => ApplicationService.ExecuteAsync(store, cmd, Streams.All, () => new[] { new TransportReady(c.TransportId, c.Kind, c.Location, c.Time) }, pub),
                    Return c => ApplicationService.ExecuteAsync<Transport, ITransportEvent>(store, cmd, Streams.All, state => state.Handle(c, Route.GetRoutes()), pub),
                    _ => Task.CompletedTask
                })
            .Triggers(async (events, d) => //TODO dispatcher that take trigger event + cmd
            {
                foreach (var e in events)
                {
                    var t = e.Event switch
                    {
                        TimePassed evt => Task.WhenAll((await Policy.Issue(e, async () =>
                            GameEngine.When(evt, await store.Projector<Transport>().ProjectAsync<ITransportEvent>(Streams.All))))
                                .Select(c => d(c))),
                        TransportReady evt => d(await Policy.Issue(e, async () => GameEngine.When(evt, (await store.GetAsync<CargoLocations>((Streams.All))).Locations))),
                        Arrived evt => Task.WhenAll((await Policy.Issue(e, async () =>
                           GameEngine.When(evt, await store.Projector<Transport>().ProjectAsync<ITransportEvent>(Streams.All)).ToArray()))
                                .Select(c => d(c))),
                        _ => Task.CompletedTask
                    };
                    await t;
                }
            })
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
