using Fiffi;
using Fiffi.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TTD.Vanilla
{
    public static class App
    {
        public static (int, IEvent[]) Run(params string[] scenarioCargo)
        {
            var cargo = scenarioCargo
                .Select((x, i) =>
                    new Cargo(i, Location.Factory, (Location)Enum.Parse(typeof(Location), x, true))).ToArray();

            var locations = new[]
            {
                new CargoLocation(Location.Factory, cargo),
                new CargoLocation(Location.Port),
                new CargoLocation(Location.A),
                new CargoLocation(Location.B),
            };

            var routes = Route.GetRoutes();

            var transports = new[]
            {
                new Transport(0, Kind.Truck, Location.Factory),
                new Transport(1, Kind.Truck, Location.Factory),
                new Transport(2, Kind.Ship, Location.Port)
            };

            var events = Array.Empty<IEvent>();

            Console.Write(events.GetCargoLocations(locations).DrawTable());

            var time = 0;
            while (!events.GetCargoLocations(locations).AllDelivered(cargo.Length))
            {
                events = events.Append(events.GetTransports(transports).Unload(time).ToArray().ToEnvelopes(x => x.TransportId.ToString()));
                events = events.Append(Load(time, events.GetCargoLocations(locations), events.GetTransports(transports), routes).ToArray().ToEnvelopes(x => x.TransportId.ToString()));
                events = events.Append(events.GetTransports(transports).Return(time, routes).ToArray().ToEnvelopes(x => x.TransportId.ToString()));

                time++;
            }

            Console.Write(events.GetTransports(transports).DrawTable());
            Console.Write(events.GetCargoLocations(locations).DrawTable());

            new FileSystemEventStore("teststore", TypeResolver.Default())
                .AppendToStreamAsync("main", events.Select(e => e.Tap(x => x.Meta.AddTypeInfo(e))).ToArray())
                .GetAwaiter().GetResult();

            return (time - 1, events.ToArray());
        }


        public static IEnumerable<Depareted> Load(
            int time,
            CargoLocation[] cargoLocations,
            Transport[] transports,
            Route[] routes)
        {
            var availableTransports = transports
                .Where(t => !t.EnRoute)
                .Where(t => !t.HasCargo)
                .Where(t => routes.GetReturnRoute(t.Kind, t.Location) == null)
                .ToList();

            foreach (var location in cargoLocations)
            {
                foreach (var item in location.Cargo)
                {
                    var t = availableTransports
                        .FirstOrDefault(x => x.Location == location.Location);

                    if (t != null)
                    {
                        availableTransports.Remove(t);
                        var route = routes.GetCargoRoute(t.Kind, t.Location, item.Destination);

                        yield return new Depareted
                        {
                            Cargo = new[] { item },
                            Destination = route.End,
                            Kind = t.Kind,
                            Location = t.Location,
                            Time = time,
                            TransportId = t.TransportId,
                            ETA = time + route.Length.Hours
                        };
                    }
                }
            }
        }
    }
}
