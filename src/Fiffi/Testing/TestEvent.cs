using System.Collections.Generic;

namespace Fiffi.Testing
{
    public class TestEvent : IEvent
    {
        public TestEvent()
        {}

        public TestEvent(AggregateId id) : this(id.Id)
        { }

        public TestEvent(IAggregateId id) : this(id.Id)
        { }

        public TestEvent(string sourceId)
        {
            SourceId = sourceId;
        }
        public string SourceId { get; set; }

        public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
    }
}
