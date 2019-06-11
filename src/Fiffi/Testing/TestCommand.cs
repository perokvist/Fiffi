using Fiffi.Validation;
using System;
using System.ComponentModel.DataAnnotations;

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

        [NotDefault]
        public IAggregateId AggregateId { get; }

        [NotDefault]
        public Guid CorrelationId { get; set; }

        [NotDefault]
        public Guid CausationId { get; set; }
    }
}
