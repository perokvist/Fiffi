using Fiffi;
using System;
using System.Linq;

namespace TTD
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

        public Transport When(EventRecord @event) => @event switch
        {
            Depareted e => When(e),
            Arrived e => When(e),
            TransportReady e => When(e),
            _ => this
        };

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
}
