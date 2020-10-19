using System.Collections.Generic;

namespace Fiffi.Testing
{
    public record TestEvent : IEvent
    {
        public TestEvent(AggregateId id)
         => (Meta, SourceId) = (new Dictionary<string, string>(), id.Id);

        public TestEvent(IAggregateId id)
         => (Meta, SourceId) = (new Dictionary<string, string>(), id.Id);

        public TestEvent(string sourceId)
         => (Meta, SourceId) = (new Dictionary<string, string>(), sourceId);

        public IDictionary<string, string> Meta { get; set; }

        public string SourceId { get; init; }
        
        public string Message { get; init; }
    };
}
