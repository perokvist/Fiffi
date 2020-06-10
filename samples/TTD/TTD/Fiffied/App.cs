using Fiffi;
using Fiffi.Modularization;
using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TTD.Vanilla;

namespace TTD.Fiffied
{
    public static class App
    {
        public static async Task<(int, IEvent[])> RunAsync(IAdvancedEventStore store, params string[] scenarioCargo)
        {
            var events = new List<IEvent>();
            Module module = null;
            //var store = new InMemoryEventStore();
            module = TTDModule.Initialize(store, async evts =>
            {
                events.AddRange(evts);
                await module.WhenAsync(evts);
            });

            var commands = scenarioCargo
                .Select((x, i) => new PlanCargo
                {
                    CargoId = i,
                    Destination = (Location)Enum.Parse(typeof(Location), x, true)
                });

            foreach (var cmd in commands)
            {
                await module.DispatchAsync(cmd);
            }

            var readyTransports = new[] {
                new ReadyTransport {
                    TransportId = 0,
                    Kind = Kind.Truck,
                    Location = Location.Factory
                },
                new ReadyTransport {
                    TransportId = 1,
                    Kind = Kind.Truck,
                    Location = Location.Factory
                },
                new ReadyTransport {
                    TransportId = 2,
                    Kind = Kind.Ship,
                    Location = Location.Port
                }
            };

            foreach (var cmd in readyTransports)
            {
                await module.DispatchAsync(cmd);
            }

            var time = 1;
            //TODO all delivered as event + projection for loop
            while (!(await module.QueryAsync(new CargoLocationQuery())).Locations.AllDelivered(scenarioCargo.Length))
            {
                await module.DispatchAsync(new AdvanceTime { Time = time });
                time++;
            }

            var l = (await module.QueryAsync(new CargoLocationQuery())).Locations;
            var b = l.AllDelivered(scenarioCargo.Length);
            Console.WriteLine(l.DrawTable());

            var t = await store.GetAsync<Transport, ITransportEvent>("all");
            Console.WriteLine(t.DrawTable());

            return (time - 1, events.ToArray());
        }
    }
}
