using Fiffi;
using Fiffi.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TTD.Vanilla
{
    public static class TransportExtensions
    {
        public static Transport[] GetTransports(this IEvent[] events, Transport[] transports)
            => transports
            .Select(t => events.Aggregate(t, (s, e) => s.When((dynamic)e)))
            .ToArray();

        public static IEnumerable<IEvent> Return(this Transport[] transports, int time, Route[] routes)
        => transports
        .Where(x => !x.EnRoute)
        .Where(x => !x.HasCargo)
        .Where(x => routes.GetReturnRoute(x.Kind, x.Location) != null)
        .Select(t =>
        {
            var route = routes.GetReturnRoute(t.Kind, t.Location);
            return new Depareted
            {
                Cargo = Array.Empty<Cargo>(),
                Destination = route.Start,
                Kind = t.Kind,
                Location = t.Location,
                Time = time,
                TransportId = t.TransportId,
                ETA = time + route.Length.Hours
            };
        });

        public static IEnumerable<IEvent> Unload(this Transport[] transports, int time)
            => transports
                .Where(x => x.EnRoute)
                .Where(x => x.ETA == time)
                .Select(x => new Arrived
                {
                    Cargo = x.Cargo,
                    Kind = x.Kind,
                    Location = x.Location,
                    Time = time,
                    TransportId = x.TransportId
                });

        public static string DrawTable(this Transport[] transports)
        {
            var table = new AsciiTable();
            table.Columns.Add(new AsciiColumn("Id", 15));
            table.Columns.Add(new AsciiColumn("Location", 65));
            table.Columns.Add(new AsciiColumn("Kind", 10));
            table.Columns.Add(new AsciiColumn("ETA", 10));

            foreach (var item in transports)
            {
                table.Rows.Add(new List<string> { item.TransportId.ToString(), item.Location.ToString(), item.Kind.ToString(), item.ETA.ToString() });
            }

            return table.ToString();
        }

    }
}
