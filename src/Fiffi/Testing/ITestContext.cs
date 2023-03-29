namespace Fiffi.Testing;

public interface ITestContext
{
    void Given(params IEvent[] events);
    Task WhenAsync(Func<Task> f);
    Task WhenAsync(ICommand command);
    Task WhenAsync(IEvent @event);
    void Then(Action<IEvent[]> f);
    Task ThenAsync(Func<IEvent[], Task> f);
    Task ThenAsync<T>(IQuery<T> q, Action<T> f) where T : class;
}
