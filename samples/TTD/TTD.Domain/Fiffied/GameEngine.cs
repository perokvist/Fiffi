using Fiffi;
using System.Linq;

namespace TTD.Domain.Fiffied
{
    public class GameEngine
    {
        public static ICommand When(TransportReady @event, CargoLocation[] cargoLocations)
        {
            var cargoInLocation = cargoLocations
                .FirstOrDefault(x => x.Location == @event.Location);
            if (cargoInLocation == null || !cargoInLocation.Cargo.Any())
                return null;

            return new PickUp
            {
                Cargo = new[] { cargoInLocation.Cargo.First() },
                Time = 0,
                TransportId = @event.TransportId
            };
        }

        public static ICommand[] When(TimePassed @event, Transport[] transports)
            => transports
                   .Where(x => x.EnRoute)
                   .Where(x => x.ETA == @event.Time)
                   .Select(x => new Unload { TransportId = x.TransportId, Location = x.Location, Time = @event.Time })
                   .ToArray();

        public static ICommand When(Arrived @event, Transport[] transports)
        {
          //  var cmd = transports //duplicate logic
          //.Where(x => x.EnRoute)
          //.Where(x => x.ETA == @event.Time)
          //.Select(x => new Unload { TransportId = x.TransportId, Location = x.Location, Time = @event.Time })
          //.FirstOrDefault();

            //if (cmd != null)
                //return cmd;

            if (@event.Cargo.First().Destination != @event.Location)
            {
                var t = transports
                  .Where(t => t.TransportId != @event.TransportId)
                  .Where(t => t.Location == @event.Location)
                  .Where(t => !t.EnRoute)
                  .Where(t => !t.HasCargo)
                  //.Where(t => routes.GetReturnRoute(t.Kind, t.Location) == null)
                  .FirstOrDefault();

                return new PickUp
                {
                    Cargo = new[] { @event.Cargo.First() },
                    Time = 0,
                    TransportId = t.TransportId
                };
            }

            return null;
        }
    }
}
