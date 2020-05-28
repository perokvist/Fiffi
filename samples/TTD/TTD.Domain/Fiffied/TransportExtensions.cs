using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTD.Domain.Fiffied
{
    public static class TransportExtensions
    {
        public static IEvent[] Handle(this Transport t, PickUp command, Route[] routes)
        {
            var route = routes.GetCargoRoute(t.Kind, t.Location, command.Cargo.First().Destination);

            return new[]
            {
                new Depareted
                {
                    Cargo = command.Cargo,
                    Destination = route.End,
                    Kind = t.Kind,
                    Location = t.Location,
                    Time = command.Time,
                    TransportId = t.TransportId,
                    ETA = command.Time + route.Length.Hours
                }
            };
        }

        public static IEvent[] Handle(this Transport t, Unload command)
         => new[] {
             new Arrived {
             Cargo = t.Cargo,
             Kind = t.Kind,
             Location = command.Location,
             Time = command.Time,
             TransportId = t.TransportId
             }
         };
    }
}
