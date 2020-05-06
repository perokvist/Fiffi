using System;
using System.Linq;

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

    public static class RouteExtensions
    {
        public static Route GetCargoRoute(this Route[] routes, Kind kind, Location start, Location destination)
        => routes
               .Where(x => x.Kind == kind)
               .Where(x => x.Start == start)
               .Where(x => x.End == destination || routes.Where(y => y.Start == x.End).Any(y => y.End == destination)) //Connects
               .Single();

        public static Route GetReturnRoute(this Route[] routes, Kind kind, Location location)
        => routes
           .Where(x => x.Kind == kind)
           .Where(x => x.End == location)
           .SingleOrDefault();

    }

}
