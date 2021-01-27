using Fiffi;
using System;
using System.Linq;

namespace TTD
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

        public CargoLocation When(EventRecord @event) => @event switch
        { 
            Depareted e => When(e),
            Arrived e => When(e),
            CargoPlanned e => When(e),
            _ => this
        };

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

}
