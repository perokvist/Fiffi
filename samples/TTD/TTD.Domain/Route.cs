using System;

namespace TTD.Domain
{
    public class Route
    {
        public Route(Location start, Location end, TimeSpan length)
        {
            Start = start;
            End = end;
            Length = length;
        }

        public Location Start { get; }
        public Location End { get; }
        public TimeSpan Length { get; }

    }

}
