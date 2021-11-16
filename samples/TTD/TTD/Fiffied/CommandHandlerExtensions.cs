using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTD.Fiffied;

public static class CommandHandlerExtensions
{
    public static EventRecord[] Handle(this Transport t, PickUp command, Route[] routes)
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

    public static EventRecord[] Handle(this Transport t, Unload command)
     => new[] {
             new Arrived {
             Cargo = t.Cargo,
             Kind = t.Kind,
             Location = command.Location,
             Time = command.Time,
             TransportId = t.TransportId
             }
     };

    public static EventRecord[] Handle(this Transport t, Return command, Route[] routes)
    {
        if (t.Location == Location.Factory)
        {
            return new[]
            {
                    new TransportReady(t.TransportId, t.Kind, t.Location, command.Time)
                };
        }

        if (t.Location == Location.Port && t.Kind == Kind.Ship)
        {
            return new[]
            {
                    new TransportReady(t.TransportId, t.Kind, t.Location, command.Time)
                };
        }

        var route = routes.GetReturnRoute(t.Kind, t.Location);

        return new[]
        {
                new Depareted
                {
                    Cargo = Array.Empty<Cargo>(),
                    Destination = route.Start,
                    Kind = t.Kind,
                    Location = t.Location,
                    Time = command.Time,
                    TransportId = t.TransportId,
                    ETA = command.Time + route.Length.Hours
                }
            };

    }
}
