using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi;

public interface IAdvancedEventStore : IEventStore, IAdvancedEventStore<IEvent>
{
    //Task DeleteStreamAsync(string streamName);
}

public interface IAdvancedEventStore<T> : IEventStore<T>
{
    Task<long> AppendToStreamAsync(string streamName, params T[] events);
    IAsyncEnumerable<T> LoadEventStreamAsAsync(string streamName, long version);
}
