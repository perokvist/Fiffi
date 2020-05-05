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
                new Route(Location.Factory, Location.Port, TimeSpan.FromHours(1)),
                new Route(Location.Port, Location.B, TimeSpan.FromHours(4)),
                new Route(Location.Factory, Location.B, TimeSpan.FromHours(5))
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
            };

            var transports = new List<Transport>
            {
                new Transport(0, Kind.Truck, Location.Factory),
                new Transport(1, Kind.Truck, Location.Factory),
                new Transport(2, Kind.Ship, Location.Port)
            };

            var events = new List<Event>();

            var time = 0;
            while (!locations.All(l => l.Location == Location.A || l.Location == Location.B))
            {
                events.AddRange(Unload(time, locations.ToArray(), transports.ToArray()));
                events.AddRange(Load(time, locations.ToArray(), transports.ToArray()));

                if (events.Any())
                {
                    locations = locations.Select(x => events.Aggregate(x, (s, e) => s.When(e))).ToList();
                    transports = transports.Select(x => events.Aggregate(x, (s, e) => s.When(e))).ToList();
                }

                time++;
            }

            return (time, events.ToArray());
        }

        public static IEnumerable<Event> Unload(int time, CargoLocation[] cargos, Transport[] transports)
        {
            var arrived = transports
                .Where(x => x.ETA != 0)
                .Where(x => x.ETA == time);
            return arrived.Select(x => new Event
            {
                EventName = EventType.ARRVIVE,
                Cargo = x.Cargo,
                Kind = x.Kind,
                Location = x.Location,
                Time = time,
                TransportId = x.TransportId
            });
        }


        public static IEnumerable<Event> Load(int time, CargoLocation[] cargos, Transport[] transports)
        {
            foreach (var location in cargos)
            {
                foreach (var cargo in location.Cargo)
                {
                    var t = transports.FirstOrDefault(x => x.Location == location.Location); //TODO same transport every time

                    yield return new Event
                    {
                        EventName = EventType.DEPART,
                        Cargo = new[] { cargo },
                        Destination = cargo.Destination,
                        Kind = t.Kind,
                        Location = t.Location,
                        Time = time,
                        TransportId = t.TransportId
                    };
                }
            }
        }


        public static IEnumerable<Transport> When(Event @event, IEnumerable<Transport> transports)
        {
            if (@event.EventName == EventType.DEPART)
            {
                var t = transports.Single(t => t.TransportId == @event.TransportId);
                t.Cargo = @event.Cargo;
            }
            return transports;
        }
    }
}
