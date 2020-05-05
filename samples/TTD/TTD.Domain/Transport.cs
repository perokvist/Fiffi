namespace TTD.Domain
{
    public class Transport
    {
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

        public Transport When(Event @event)
        {
            if (@event.EventName == EventType.DEPART && this.TransportId == @event.TransportId)
                return new Transport(@event.TransportId, @event.Kind, @event.Location)
                {
                    ETA = 4, //TODO missing
                    Cargo = @event.Cargo,
                    Kind = @event.Kind,
                    Location = @event.Destination,
                    TransportId = @event.TransportId
                };

            return this;
        }
    }

}
