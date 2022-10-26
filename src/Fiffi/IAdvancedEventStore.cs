namespace Fiffi;

public interface IAdvancedEventStore : IEventStore, IAdvancedEventStore<IEvent>
{}

public interface IAdvancedEventStore<T> : IEventStore<T>
{
    Task<long> AppendToStreamAsync(string streamName, params T[] events);
    IAsyncEnumerable<T> LoadEventStreamAsAsync(string streamName, long version);
    IAsyncEnumerable<T> LoadEventStreamAsAsync(string streamName, params IStreamFilter[] filters);
}

public interface IStreamFilter
{ };

public record DateStreamFilter(DateTime StartDate, DateTime EndDate) : IStreamFilter;
public record CategoryStreamFilter(string CategoryName) : IStreamFilter;

