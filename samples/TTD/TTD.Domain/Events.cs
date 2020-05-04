using Fiffi;
using System;
using System.Collections.Generic;

namespace TTD.Domain
{

    public class Event : IEvent
    {
        public EventType EventName { get; set; }
        public int Time { get; set; }
        public int TransportId { get; set; }
        public Kind Kind { get; set; }
        public Location Location { get; set; }
        public Location Destination { get; set; }
        public Cargo[] Cargo { get; set; }

        public string SourceId { get; set; } = Guid.NewGuid().ToString();

        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    }

    public class Cargo
    {
        public int CargoId { get; set; }
        public Location Destination { get; set; }
        public Location Origin { get; set; }
    }

    public enum EventType
    { 
        ARRVIVE = 10,
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
