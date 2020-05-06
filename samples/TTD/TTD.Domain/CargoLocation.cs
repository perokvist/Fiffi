using Fiffi.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TTD.Domain
{
    public class CargoLocation
    {
        public CargoLocation(Location location) : this(location, Array.Empty<Cargo>())
        {}

        public CargoLocation(Location location, Cargo[] cargo)
        {
            Location = location;
            Cargo = cargo;
        }

        public Location Location { get; }
        public Cargo[] Cargo { get; }

        public CargoLocation When(Event @event)
        {
            if (@event.EventName == EventType.DEPART && @event.Location == Location)
                return new CargoLocation(
                    Location,
                    Cargo.Where(c => !@event.Cargo.Any(x => x.CargoId == c.CargoId)).ToArray()
                );

            if (@event.EventName == EventType.ARRIVE && @event.Location == Location)
                return new CargoLocation
                (
                    @event.Location,
                    Cargo.Concat(@event.Cargo).ToArray()
                );

            return this;
        }
    }

    public static class CargoLocationExtensions
    {
        public static CargoLocation[] GetCargoLocations(this Event[] events, CargoLocation[] cargoLocations)
        => cargoLocations
        .Select(t => events.Aggregate(t, (s, e) => s.When(e)))
        .ToArray();

        public static bool AllDelivered(this CargoLocation[] cargoLocations)
            => cargoLocations
            .Where(l => l.Location != Location.A) 
            .Where(l => l.Location != Location.B)
            .All(x => !x.Cargo.Any());

        public static bool AllDelivered(this CargoLocation[] cargoLocations, Cargo[] cargo)
        {
            var actual = cargoLocations
                .GroupBy(x => x.Location)
                .Select(x => new { Location = x.Key, Count = x.Sum(z => z.Cargo.Length) });

            var expected = cargo
                .GroupBy(x => x.Destination)
                .Select(x => new { Location = x.Key, Count = x.Count() });

            return expected.All(x => x.Count == actual.Single(cl => cl.Location == x.Location).Count);
        }

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

}
