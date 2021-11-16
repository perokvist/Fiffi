using Fiffi;
using System.Collections.Generic;
using System.Linq;

namespace TTD.Fiffied;

public static class GameEngine
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
            Time = @event.Time,
            TransportId = @event.TransportId
        };
    }

    public static ICommand[] When(TimePassed @event, Transport[] transports)
        => transports
               .Where(x => x.EnRoute)
               .Where(x => x.ETA == @event.Time)
               .Select(x => new Unload { TransportId = x.TransportId, Location = x.Location, Time = @event.Time })
               .ToArray();

    public static IEnumerable<ICommand> When(Arrived @event, Transport[] transports)
    {
        yield return new Return
        {
            Time = @event.Time,
            TransportId = @event.TransportId
        };

        if (!@event.Cargo.Any())
            yield break;

        if (@event.Cargo.First().Destination != @event.Location)
        {
            var availableTransport = transports
              .Where(t => t.TransportId != @event.TransportId)
              .Where(t => t.Location == @event.Location)
              .Where(t => !t.EnRoute)
              .Where(t => !t.HasCargo)
              .FirstOrDefault();

            if (availableTransport == null)
                yield break;

            yield return new PickUp
            {
                Cargo = new[] { @event.Cargo.First() },
                Time = @event.Time,
                TransportId = availableTransport.TransportId
            };
        }

        yield break;
    }
}
