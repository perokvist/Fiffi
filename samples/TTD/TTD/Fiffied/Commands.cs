using Fiffi;
using System;

namespace TTD.Fiffied
{
    public class Return : ICommand
    {
        public int TransportId { get; set; }
        public int Time { get; set; }
        IAggregateId ICommand.AggregateId => new AggregateId(TransportId.ToString());
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class AdvanceTime : ICommand
    {
        public int Time { get; set; }
        IAggregateId ICommand.AggregateId => new AggregateId(Time.ToString());
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class PlanCargo : ICommand
    {
        public int CargoId { get; set; }
        public Location Destination { get; set; }
        IAggregateId ICommand.AggregateId => new AggregateId(CargoId.ToString());
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class ReadyTransport : ICommand
    {
        public int TransportId { get; set; }
        public Kind Kind { get; set; }
        public Location Location { get; set; }
        public int Time { get; set; }

        IAggregateId ICommand.AggregateId => new AggregateId(TransportId.ToString());
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }
    public class PickUp : ICommand
    {
        public int TransportId { get; set; }
        public Cargo[] Cargo { get; set; }
        public int Time { get; set; }
        IAggregateId ICommand.AggregateId => new AggregateId(TransportId.ToString());
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class Unload : ICommand
    {
        public int TransportId { get; set; }
        public Cargo[] Cargo { get; set; }
        public int Time { get; set; }
        public Location Location { get; internal set; }

        IAggregateId ICommand.AggregateId => new AggregateId(TransportId.ToString());
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }
}
