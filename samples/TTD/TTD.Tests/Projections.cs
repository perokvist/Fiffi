using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TTD.Domain;
using Xunit;

namespace TTD.Tests
{
    public class Projections
    {
        [Fact]
        public async Task AB_Projections()
        {
            var events = new List<Event>
            {
                new Event {
                    EventName = EventType.DEPART,
                    Time = 0,
                    TransportId = 0,
                    Kind = Kind.Truck,
                    Location = Location.Factory,
                    Destination = Location.Port,
                    Cargo = new [] { new Cargo(0, Location.A, Location.Factory) }
                },
               new Event {
                    EventName = EventType.DEPART,
                    Time = 0,
                    TransportId = 1,
                    Kind = Kind.Truck,
                    Location = Location.Factory,
                    Destination = Location.B,
                    Cargo = new [] { new Cargo(1, Location.B, Location.Factory) }
                },
               new Event {
                    EventName = EventType.ARRVIVE,
                    Time = 1,
                    TransportId = 0,
                    Kind = Kind.Truck,
                    Location = Location.Port,
                    Cargo = new [] { new Cargo(0, Location.A, Location.Factory) }
                },
               new Event {
                    EventName = EventType.DEPART,
                    Time = 1,
                    TransportId = 0,
                    Kind = Kind.Truck,
                    Location = Location.Port,
                    Destination = Location.Factory
                },
               new Event {
                    EventName = EventType.DEPART,
                    Time = 1,
                    TransportId = 2,
                    Kind = Kind.Ship,
                    Location = Location.Port,
                    Destination = Location.A,
                    Cargo = new [] { new Cargo(0, Location.A, Location.Factory) }
                },
               new Event {
                    EventName = EventType.ARRVIVE,
                    Time = 2,
                    TransportId = 0,
                    Kind = Kind.Truck,
                    Location = Location.Factory
                },
               new Event {
                    EventName = EventType.ARRVIVE,
                    Time = 5,
                    TransportId = 1,
                    Kind = Kind.Truck,
                    Location = Location.B,
                    Cargo = new [] { new Cargo(1, Location.B, Location.Factory) }
                },
                new Event {
                    EventName = EventType.DEPART,
                    Time = 5,
                    TransportId = 1,
                    Kind = Kind.Truck,
                    Location = Location.B,
                    Destination = Location.Factory
                },
                new Event {
                    EventName = EventType.DEPART,
                    Time = 5,
                    TransportId = 1,
                    Kind = Kind.Truck,
                    Location = Location.B,
                    Destination = Location.Factory
                },
                new Event {
                    EventName = EventType.ARRVIVE,
                    Time = 5,
                    TransportId = 2,
                    Kind = Kind.Ship,
                    Location = Location.A,
                    Cargo = new [] { new Cargo(0, Location.A, Location.Factory) }
                },
                new Event {
                    EventName = EventType.DEPART,
                    Time = 5,
                    TransportId = 2,
                    Kind = Kind.Ship,
                    Location = Location.A,
                    Destination = Location.Port
                },
            };

            var eventsWithMeta = events.Select(e => e.Tap(x => e.Meta.AddTypeInfo(e))).ToArray();
            var store = new Fiffi.FileSystem.FileSystemEventStore("teststore", TypeResolver.FromMap(TypeResolver.GetEventsFromTypes(typeof(Event))));
            _ = await store.AppendToStreamAsync("all", eventsWithMeta);

            //{ "event": "DEPART", "time": 0, "transport_id": 0, "kind": "TRUCK", "location": "FACTORY", "destination": "PORT", "cargo": [{"cargo_id": 0, "destination": "A", "origin": "FACTORY"}]}
            //{"event": "DEPART", "time": 0, "transport_id": 1, "kind": "TRUCK", "location": "FACTORY", "destination": "B", "cargo": [{"cargo_id": 1, "destination": "B", "origin": "FACTORY"}]}
            //{"event": "ARRIVE", "time": 1, "transport_id": 0, "kind": "TRUCK", "location": "PORT", "cargo": [{"cargo_id": 0, "destination": "A", "origin": "FACTORY"}]}
            //{"event": "DEPART", "time": 1, "transport_id": 0, "kind": "TRUCK", "location": "PORT", "destination": "FACTORY"}
            //{"event": "DEPART", "time": 1, "transport_id": 2, "kind": "SHIP", "location": "PORT", "destination": "A", "cargo": [{"cargo_id": 0, "destination": "A", "origin": "FACTORY"}]}
            //{"event": "ARRIVE", "time": 2, "transport_id": 0, "kind": "TRUCK", "location": "FACTORY"}
            //{"event": "ARRIVE", "time": 5, "transport_id": 1, "kind": "TRUCK", "location": "B", "cargo": [{"cargo_id": 1, "destination": "B", "origin": "FACTORY"}]}
            //{"event": "DEPART", "time": 5, "transport_id": 1, "kind": "TRUCK", "location": "B", "destination": "FACTORY"}
            //{"event": "ARRIVE", "time": 5, "transport_id": 2, "kind": "SHIP", "location": "A", "cargo": [{"cargo_id": 0, "destination": "A", "origin": "FACTORY"}]}
            //{"event": "DEPART", "time": 5, "transport_id": 2, "kind": "SHIP", "location": "A", "destination": "PORT"}
        }
    }
}
