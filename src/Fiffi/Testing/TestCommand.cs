using System;

namespace Fiffi.Testing
{
    public class TestCommand : ICommand
    {
        public TestCommand(IAggregateId id) : this(id.ToString())
        { }

        readonly string id;

        public TestCommand(string id)
        {
            this.id = id;
        }

        public IAggregateId AggregateId => new AggregateId(this.id);

        public Guid CorrelationId { get; private set; } = Guid.NewGuid();

    }
}
