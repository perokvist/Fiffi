using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTD.Domain
{
    public static class Main
    {
        public static (int, Event[]) Run(params string[] scenarioCargo)
        {
            var locations = new[]
   {
                new CargoLocation
                {
                    Location = Location.Factory,
                    Cargo = scenarioCargo.Select(x =>
                        new Cargo(0, Location.Factory, (Location) Enum.Parse(typeof(Location) , x, true))
                    ).ToArray()
                }
            };

            var routes = new List<Route> {
                new Route(Location.Factory, Location.Port, TimeSpan.FromHours(1), Kind.Truck),
                new Route(Location.Port, Location.A, TimeSpan.FromHours(4), Kind.Ship),
                new Route(Location.Factory, Location.B, TimeSpan.FromHours(5), Kind.Truck)
            };

            var transports = new[]
            {
                new Transport(0, Kind.Truck, Location.Factory),
                new Transport(1, Kind.Truck, Location.Factory),
                new Transport(2, Kind.Ship, Location.Port)
            };

            var events = new List<Event>();

            var time = 0;
            while (!events.ToArray().GetCargoLocations(locations.ToArray()).AllDelivered())
            {
                events.AddRange(events.ToArray().GetTransports(transports).Unload(time));
                events.AddRange(Load(time, events.ToArray().GetCargoLocations(locations), events.ToArray().GetTransports(transports), routes.ToArray()));
                events.AddRange(events.ToArray().GetTransports(transports).Return(time, routes.ToArray()));

                time++;
            }

            return (time - 1, events.ToArray());
        }


        public static IEnumerable<Event> Load(
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

                        yield return new Event
                        {
                            EventName = EventType.DEPART,
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
