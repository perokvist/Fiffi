using Fiffi;
using System.Linq;
using System.Threading.Tasks;

namespace TTD.Fiffied
{
    public static class StoreExtensions
    {
        public static async Task<Transport[]> GetTransports(this IEventStore store, string streamName)
        {
            var r = await store.LoadEventStreamAsync(streamName, 0);
            return r.Events
                .OfType<ITransportEvent>()
                .GroupBy(x => x.SourceId)
                .Select(x => x.Rehydrate<Transport>())
                .ToArray();
        }
    }
}
