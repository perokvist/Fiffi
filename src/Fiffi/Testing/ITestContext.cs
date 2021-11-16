using System;
using System.Threading.Tasks;
using static Fiffi.Testing.TestContext;

namespace Fiffi.Testing;

public interface ITestContext
{
    void Given(params IEvent[] events);
    void Then(Action<IEvent[]> f);
    Task WhenAsync(Func<Task> f);
    Task WhenAsync(ICommand command);
    Task WhenAsync(IEvent @event);
}
