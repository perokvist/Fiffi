using Fiffi;
using Fiffi.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TTD.Domain
{
    public class Transport
    {
        public Transport()
        {}

        public Transport(int transportId, Kind kind, Location location)
        {
            TransportId = transportId;
            Kind = kind;
            Location = location;
        }
        public int TransportId { get; set; }
        public Kind Kind { get; set; }
        public Location Location { get; set; }
        public int ETA { get; set; }
        public Cargo[] Cargo { get; set; }
        public bool EnRoute => ETA != 0;
        public bool HasCargo => Cargo != null && Cargo.Any();

        public Transport When(IEvent @event) => this;

        public Transport When(Depareted @event)
            => this.TransportId == @event.TransportId ?
                new Transport(@event.TransportId, @event.Kind, @event.Location)
                {
                    ETA = @event.ETA,
                    Cargo = @event.Cargo,
                    Kind = @event.Kind,
                    Location = @event.Destination,
                    TransportId = @event.TransportId
                } : this;

        public Transport When(Arrived @event)
           => this.TransportId == @event.TransportId ?
                new Transport(@event.TransportId, @event.Kind, @event.Location)
                {
                    ETA = 0,
                    Cargo = Array.Empty<Cargo>(),
                    Kind = @event.Kind,
                    Location = @event.Location,
                    TransportId = @event.TransportId
                } : this;

        public Transport When(TransportReady @event)
        { 
            if(this.Kind == 0)
                return new Transport(@event.TransportId, @event.Kind, @event.Location);

            return this;
        }
    }


    public static class TransportExtensions
    {
        public static async Task<Transport[]> GetTransports(this IEventStore store, string streamName)
        {
            var r = await store.LoadEventStreamAsync(streamName, 0);
            return r.Events
                .OfType<ITransportEvent>()
                .GroupBy(x => x.SourceId)
                .Select(x => x.Rehydrate<Transport>())
                .ToArray();
        }


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
