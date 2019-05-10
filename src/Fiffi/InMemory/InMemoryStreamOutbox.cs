using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
    public class InMemoryStreamOutbox : IStreamOutbox
    {
        private IDictionary<string, StreamPointer> publish = new ConcurrentDictionary<string, StreamPointer>();

        public Task CancelAsync(string sourceId)
            => CompleteAsync(sourceId);

        public Task CompleteAsync(string sourceId)
        {
            if (!publish.ContainsKey(sourceId)) return Task.CompletedTask;

            publish.Remove(sourceId);
            return Task.CompletedTask;
        }

        public Task<StreamPointer[]> GetAllPendingAsync()
            => Task.FromResult(publish.Select(x => x.Value).ToArray());

        public Task<StreamPointer> GetPendingAsync(string sourceId)
        {
            if (!this.publish.ContainsKey(sourceId)) return Task.FromResult<StreamPointer>(null);

            return Task.FromResult(this.publish[sourceId]);
        }

        public Task PendingAsync(IAggregateId id, string streamName, long expectedNewVersion)
        {
            if (this.publish.ContainsKey(id.Id))
                throw new DBConcurrencyException($"There is already a task pending for {id.Id}");

            this.publish.Add(id.Id, new StreamPointer(id.Id, streamName, expectedNewVersion));
            return Task.CompletedTask;
        }
    }
}
