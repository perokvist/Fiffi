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
            var routes = new List<Route> {
                new Route(Location.Factory, Location.Port, TimeSpan.FromHours(1), Kind.Truck),
                new Route(Location.Port, Location.A, TimeSpan.FromHours(4), Kind.Ship),
                new Route(Location.Factory, Location.B, TimeSpan.FromHours(5), Kind.Truck)
            };

            var locations = new List<CargoLocation>
            {
                new CargoLocation
                {
                    Location = Location.Factory,
                    Cargo = scenarioCargo.Select(x =>
                        new Cargo(0, Location.Factory, (Location) Enum.Parse(typeof(Location) , x, true))
                    ).ToArray()
                }
            }.ToArray();

            var events = new List<Event>();

            var time = 0;
            while (!GetCargoLocations(events.ToArray(), locations).All(l => l.Location == Location.A || l.Location == Location.B))
            {
                events.AddRange(Unload(time, events.ToArray().GetCargoLocations(locations), GetTransports(events.ToArray())));
                events.AddRange(Load(time, events.ToArray().GetCargoLocations(locations), GetTransports(events.ToArray()), routes.ToArray()));
                events.AddRange(Return(time, GetTransports(events.ToArray()), routes.ToArray()));

                time++;
            }

            return (time - 1, events.ToArray());
        }

        public static IEnumerable<Event> Return(int time, Transport[] transports, Route[] routes)
         => transports
               .Where(x => !x.EnRoute)
               .Where(x => !x.HasCargo)
               .Where(x => routes.GetReturnRoute(x.Kind, x.Location) != null)
               .Select(t =>
               {
                   var route = routes.GetReturnRoute(t.Kind, t.Location);
                   return new Event
                   {
                       EventName = EventType.DEPART,
                       Cargo = Array.Empty<Cargo>(),
                       Destination = route.Start,
                       Kind = t.Kind,
                       Location = t.Location,
                       Time = time,
                       TransportId = t.TransportId,
                       ETA = time + route.Length.Hours
                   };
               });


        public static IEnumerable<Event> Unload(int time, CargoLocation[] cargos, Transport[] transports)
        {
            var arrived = transports
                .Where(x => x.EnRoute)
                .Where(x => x.ETA == time);
            return arrived.Select(x => new Event
            {
                EventName = EventType.ARRIVE,
                Cargo = x.Cargo,
                Kind = x.Kind,
                Location = x.Location,
                Time = time,
                TransportId = x.TransportId
            });
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

        public static Transport[] GetTransports(this Event[] events)
            => new List<Transport>
            {
                new Transport(0, Kind.Truck, Location.Factory),
                new Transport(1, Kind.Truck, Location.Factory),
                new Transport(2, Kind.Ship, Location.Port)
            }
            .Select(t => events.Aggregate(t, (s, e) => s.When(e)))
            .ToArray();

        public static CargoLocation[] GetCargoLocations(this Event[] events, CargoLocation[] cargoLocations)
            => cargoLocations
            .Select(t => events.Aggregate(t, (s, e) => s.When(e)))
            .ToArray();

        public static Route GetCargoRoute(this Route[] routes, Kind kind, Location start, Location destination)
        => routes
               .Where(x => x.Kind == kind)
               .Where(x => x.Start == start)
               .Where(x => x.End == destination || routes.Where(y => y.Start == x.End).Any(y => y.End == destination)) //Connects
               .Single();

        public static Route GetReturnRoute(this Route[] routes, Kind kind, Location location)
        => routes
           .Where(x => x.Kind == kind)
           .Where(x => x.End == location)
           .SingleOrDefault();
    }
}
