using System;

namespace TTD;

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

    public static Route[] GetRoutes()
        => new[] {
                new Route(Location.Factory, Location.Port, TimeSpan.FromHours(1), Kind.Truck),
                new Route(Location.Port, Location.A, TimeSpan.FromHours(4), Kind.Ship),
                new Route(Location.Factory, Location.B, TimeSpan.FromHours(5), Kind.Truck)
        };
}
