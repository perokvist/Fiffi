using Fiffi;
using Fiffi.FileSystem;
using Fiffi.Modularization;
using Fiffi.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TTD.Domain.Fiffied
{
    public static class MainFiffi
    {
        public static async Task<(int, IEvent[])> RunAsync(params string[] scenarioCargo)
        {
            var events = new List<IEvent>();
            Module module = null;
            module = TTDModule.Initialize(new InMemoryEventStore(), async evts =>
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

            return (time -1, events.ToArray());
        }
    }
}
