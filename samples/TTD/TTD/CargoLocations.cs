using Fiffi;
using System.Collections.Generic;
using System.Linq;

namespace TTD
{
    public class CargoLocations
    {
        public CargoLocations()
        {
            this.inner = new Dictionary<Location, CargoLocation>();
        }
        public CargoLocation[] Locations => inner.Values.ToArray();

        private readonly IDictionary<Location, CargoLocation> inner;

        public CargoLocations When(EventRecord @event) => @event switch
        {
            Depareted e => When(e),
            Arrived e => When(e),
            CargoPlanned e => When(e),
            _ => this
        };

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

}
