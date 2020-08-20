using System.Threading.Tasks;

namespace Fiffi
{
    public interface IAdvancedEventStore : IEventStore
    {
        Task<long> AppendToStreamAsync(string streamName, params IEvent[] events);

        //Task DeleteStreamAsync(string streamName);
    }
}
