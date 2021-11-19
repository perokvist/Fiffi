namespace Fiffi.Testing;

public record TestState
{
    public TestState When(EventRecord @event) => this.Tap(x => x.Applied.Add(@event));

    public TestState When(IEvent @event) => this.Tap(x => x.Applied.Add(@event.Event));
    public long Version { get; set; }

    public List<EventRecord> Applied { get; internal set; } = new();
}
