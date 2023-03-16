namespace Fiffi.Modularization;

public interface IModule
{
    Task DispatchAsync(ICommand command);
    Task<T> QueryAsync<T>(IQuery<T> q) where T : class; 
    IAsyncEnumerable<T> QueryAsync<T>(IStreamQuery<T> q);
    Task WhenAsync(params IEvent[] events);
    Task OnStart(IEvent[] events);
}

public record View();