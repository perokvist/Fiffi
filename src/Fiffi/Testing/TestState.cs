using System.Collections.Generic;

namespace Fiffi.Testing
{
    public class TestState
    {
        public TestState When(IEvent @event) => this.Tap(x => x.Applied.Add(@event));
        public long Version { get; set; }

        public List<IEvent> Applied { get; internal set; } = new List<IEvent>();
    }
}
