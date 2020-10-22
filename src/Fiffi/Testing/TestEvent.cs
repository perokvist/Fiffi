using System.Collections.Generic;

namespace Fiffi.Testing
{
    public record TestEvent : IEvent
    {
        public TestEvent(AggregateId id) : this(id.Id)
        { }

        public TestEvent(IAggregateId id) : this(id.Id)
        {}

        public TestEvent(string sourceId)
         => (Meta, SourceId, Message) = (new Dictionary<string, string>(), sourceId, "Test Message");

        public IDictionary<string, string> Meta { get; set; }

        public string SourceId { get; init; }
        
        public string Message { get; init; }

        public EventRecord Event { get; init; }
    };

    public record TestEventRecord(string Message) : EventRecord;
}
