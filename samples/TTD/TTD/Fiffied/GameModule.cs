﻿using Fiffi;
using Fiffi.Modularization;
using System;
using System.Threading.Tasks;
using Fiffi.Projections;
using System.Linq;
using TTD;
using System.Security.Cryptography.X509Certificates;

namespace TTD.Fiffied;

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
        .Triggers(async (events, d) =>
        {
            await foreach (var t in events.Select(async e =>
                e.Event switch
                {
                    TimePassed evt => GameEngine.When(evt, await store.Projector<Transport>().ProjectAsync<ITransportEvent>(Streams.All)).Dispatch(e, d),
                    TransportReady evt => d(e, GameEngine.When(evt, (await store.GetAsync<CargoLocations>((Streams.All))).Locations)),
                    Arrived evt => GameEngine.When(evt, await store.Projector<Transport>().ProjectAsync<ITransportEvent>(Streams.All)).Dispatch(e, d),
                    _ => Task.CompletedTask
                }).ToAsyncEnumerable()) ;
        })
        .Query<CargoLocationQuery, CargoLocations>(q => store.Projector<CargoLocations>().ProjectAsync(Streams.All))
        .Create(store);
}

public class Streams
{
    public const string All = "all";
}

public class CargoLocationQuery : IQuery<CargoLocations>
{ }
