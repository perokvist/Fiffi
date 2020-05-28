using Fiffi;
using Fiffi.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TTD.Domain
{
    public class CargoLocation
    {
        public CargoLocation() : this(Location.Factory)
        { }

        public CargoLocation(Location location) : this(location, Array.Empty<Cargo>())
        { }

        public CargoLocation(Location location, Cargo[] cargo)
        {
            Location = location;
            Cargo = cargo;
        }

        public Location Location { get; }
        public Cargo[] Cargo { get; }

        public CargoLocation When(IEvent @event) => this;

        public CargoLocation When(Depareted @event)
            => @event.Location == Location ?
                new CargoLocation(
                    Location,
                    Cargo.Where(c => !@event.Cargo.Any(x => x.CargoId == c.CargoId)).ToArray()
                    )
                : this;

        public CargoLocation When(Arrived @event)
            => @event.Location == Location ?
                new CargoLocation(
                    @event.Location,
                    Cargo.Concat(@event.Cargo).ToArray()
                    )
                : this;

        public CargoLocation When(CargoPlanned @event)
            => new CargoLocation(@event.Origin, Cargo.Concat(new[] { new Cargo(@event.CargoId, @event.Origin, @event.Destination) }).ToArray());
    }

    public class CargoLocations
    {
        public CargoLocations()
        {
            this.inner = new Dictionary<Location, CargoLocation>();
        }
        public CargoLocation[] Locations => inner.Values.ToArray();

        private readonly IDictionary<Location, CargoLocation> inner;

        public CargoLocations When(IEvent @event) => this;
        public CargoLocations When(Depareted @event)
        {
            if (!inner.ContainsKey(@event.Location))
                inner.Add(@event.Location, new CargoLocation(@event.Location));

            var p = inner[@event.Location];
            inner[@event.Location] = p.When(@event);
            return this;
        }
        public CargoLocations When(Arrived @event)
        {
            if(!inner.ContainsKey(@event.Location))
                inner.Add(@event.Location, new CargoLocation(@event.Location));


            var p = inner[@event.Location];
            inner[@event.Location] = p.When(@event);
            return this;
        }
        public CargoLocations When(CargoPlanned @event)
        {
            if (inner.ContainsKey(@event.Origin))
                inner[@event.Origin] = inner[@event.Origin].When(@event);
            else
                inner.Add(@event.Origin, new CargoLocation().When(@event));
            return this;
        }
    }

    public static class CargoLocationExtensions
    {
        public static CargoLocation[] GetCargoLocations(this IEvent[] events, CargoLocation[] cargoLocations)
        => cargoLocations
        .Select(t => events.Aggregate(t, (s, e) => s.When((dynamic)e)))
        .ToArray();

        public static bool AllDelivered(this CargoLocation[] cargoLocations, int cargo)
            => cargoLocations
            .Sum(x => x.Cargo.Count(c => c.Destination == x.Location)) == cargo;

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
