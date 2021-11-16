using Fiffi;
using Fiffi.Visualization;
using System.Collections.Generic;
using System.Linq;

namespace TTD;

public static class CargoLocationExtensions
{
    public static CargoLocation[] GetCargoLocations(this IEvent[] events, CargoLocation[] cargoLocations)
    => cargoLocations
    .Select(t => events.Aggregate(t, (s, e) => s.When((dynamic)e)))
    .ToArray();

    public static bool AllDelivered(this CargoLocation[] cargoLocations, int cargo)
        => cargoLocations
        .Sum(x => x.Cargo.Count(c => c.Destination == x.Location)) == cargo;

    public static string DrawTable(this CargoLocation[] cargoLocations)
    {
        var table = new AsciiTable();
        table.Columns.Add(new AsciiColumn("Location", 50));
        table.Columns.Add(new AsciiColumn("Cargo", 50));

        foreach (var item in cargoLocations)
        {
            table.Rows.Add(new List<string> { item.Location.ToString(), item.Cargo.Select(x => x.Destination.ToString()).Aggregate(string.Empty, (r, l) => $"{l}{r}") });
        }

        return table.ToString();
    }
}
