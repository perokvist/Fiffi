using System.Linq;

namespace TTD
{
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
