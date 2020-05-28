using Fiffi;
using System;
using System.Collections.Generic;

namespace TTD.Domain
{
    public interface ITransportEvent : IEvent { }

    public class Depareted : ITransportEvent
    {
        public int Time { get; set; }
        public int TransportId { get; set; }
        public Kind Kind { get; set; }
        public Location Location { get; set; }
        public Location Destination { get; set; }
        public Cargo[] Cargo { get; set; }
        public int ETA { get; set; }
        public string SourceId => TransportId.ToString();
        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    }

    public class Arrived : ITransportEvent
    {
        public int Time { get; set; }
        public int TransportId { get; set; }
        public Kind Kind { get; set; }
        public Location Location { get; set; }
        public Cargo[] Cargo { get; set; }
        public string SourceId => TransportId.ToString();
        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    }

    public class TransportReady : ITransportEvent
    {
        public TransportReady(int transportId, Kind kind, Location location)
        {
            TransportId = transportId;
            Kind = kind;
            Location = location;
        }
        public int TransportId { get; set; }
        public Kind Kind { get; set; }
        public Location Location { get; set; }
        public string SourceId => TransportId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }

    public class CargoPlanned : IEvent
    {
        public CargoPlanned(int CargoId, Location Destination, Location origin)
        {
            this.CargoId = CargoId;
            this.Destination = Destination;
            Origin = origin;
        }

        public int CargoId { get; set; }
        public Location Destination { get; set; }
        public Location Origin { get; set; }
        public string SourceId => CargoId.ToString();
        public IDictionary<string, string> Meta { get; set; }
    }


    public enum EventType
    { 
        ARRIVE = 10,
        DEPART = 20
    }

    public enum Kind
    { 
        Truck = 10,
        Ship = 20
    }

    public enum Location
    { 
        Factory = 0,
        Port = 10,
        A = 20,
        B = 30
    }

}
