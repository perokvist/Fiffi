using System.Linq;

namespace TTD.Domain
{
    public class CargoLocation
    {
        public Location Location { get; set; }
        public Cargo[] Cargo { get; set; }

        public CargoLocation When(Event @event)
        {
            if (@event.EventName == EventType.DEPART)
                return new CargoLocation
                {
                    Location = Location,
                    Cargo = Cargo.Where(c => !@event.Cargo.Any(x => x.CargoId == c.CargoId)).ToArray()
                };

            if (@event.EventName == EventType.ARRIVE)
                return new CargoLocation
                {
                    Location = @event.Location,
                    Cargo = Cargo.Concat(@event.Cargo).ToArray()
                };

            return this;
        }
    }

}
