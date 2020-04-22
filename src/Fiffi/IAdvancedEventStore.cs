using System.Threading.Tasks;

namespace Fiffi
{
    public interface IAdvancedEventStore : IEventStore
    {
        Task<long> AppendToStreamAsync(string streamName, IEvent[] events);

        //Task DeleteStreamAsync(string streamName);
    }

    //public interface IAdvancedEventStore<T> : IAdvancedEventStore
    //{
    //    public T InnerStore { get; }
    //}
}
