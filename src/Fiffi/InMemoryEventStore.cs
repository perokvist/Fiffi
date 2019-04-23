using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
    public class InMemoryEventStore : IEventStore
    {
        readonly IDictionary<string, IEvent[]> store = new ConcurrentDictionary<string, IEvent[]>();

        public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events)
        {
            var currentEvents = store.ContainsKey(streamName) ? store[streamName] : new IEvent[] { };

            if (currentEvents.Any() && currentEvents.Last().GetVersion() != version)
                throw new DBConcurrencyException($"wrong version - expected {version} but was {currentEvents.Last().GetVersion()}");

            var newStream = currentEvents.Concat(events).ToArray();
            store[streamName] = newStream;
            return Task.FromResult(newStream.Last().GetVersion());
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<(IEnumerable<IEvent>, long)> LoadEventStreamAsync(string streamName, long version) =>
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            store.ContainsKey(streamName) ? (store[streamName].Where(x => x.GetVersion() >= version).ToArray(), 0) : (new IEvent[] { }, 0);
    }
}
