using System.Threading.Tasks;

namespace Fiffi.Projections
{
    public class Projector<T>
              where T : class, new()
    {
        private IEventStore store;

        public Projector(IEventStore store)
            => this.store = store;

        public Task<T> ProjectAsync(string streamName)
            => this.store.GetAsync<T>(streamName);

        public Task<T> ProjectAsync<TStream>(AggregateId id)
            => ProjectAsync<TStream>((IAggregateId)id);

        public Task<T> ProjectAsync<TStream>(IAggregateId id)
            => this.store.GetAsync<T, TStream>(id);
    }
}
