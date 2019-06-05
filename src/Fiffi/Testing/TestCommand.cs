using System;

namespace Fiffi.Testing
{
    public class TestCommand : ICommand
    {
        public TestCommand(AggregateId id) : this((IAggregateId)id)
        { }

        public TestCommand(IAggregateId id)
        {
            var c = Guid.NewGuid();
            this.CorrelationId = c;
            this.CausationId = c;
            AggregateId = id;
        }
        public IAggregateId AggregateId { get; }

        public Guid CorrelationId { get; set; }
        public Guid CausationId { get; set; }
    }
}
