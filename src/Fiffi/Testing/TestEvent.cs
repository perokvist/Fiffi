using System.Collections.Generic;

namespace Fiffi.Testing;

public class TestEvent : EventEnvelope<TestEventRecord>
{
    public TestEvent(AggregateId id) : this(id.Id)
    { }

    public TestEvent(IAggregateId id) : this(id.Id)
    { }

    public TestEvent(string sourceId) : base(sourceId, new TestEventRecord("Test Message"))
    {
        Message = "Test Message";
    }

    public string Message { get; set; }

};

public record TestEventRecord(string Message) : EventRecord;
