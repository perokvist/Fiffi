using System;

namespace TTD.Domain
{
    public class Route
    {
        public Route(Location start, Location end, TimeSpan length, Kind kind)
        {
            Start = start;
            End = end;
            Length = length;
            Kind = kind;
        }

        public Location Start { get; }
        public Location End { get; }
        public TimeSpan Length { get; }
        public Kind Kind { get; set; }
    }

}
