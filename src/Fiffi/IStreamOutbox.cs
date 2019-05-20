using System;
using System.Threading.Tasks;

namespace Fiffi
{
    public interface IStreamOutbox
    {
        Task PendingAsync(IAggregateId id, string streamName, long version, params IEvent[] events);
        Task<StreamPointer> GetPendingAsync(string sourceId);
        Task<StreamPointer[]> GetAllPendingAsync();
        Task CancelAsync(string sourceId, params IEvent[] events);
        Task CompleteAsync(string sourceId, params IEvent[] events);
    }

    public class StreamPointer 
    {
        public StreamPointer(string sourceId, string streamName, long version)
        {
            SourceId = sourceId;
            StreamName = streamName;
            Version = version;
            Status = StreamPointerStatus.Pending;
        }

        public string SourceId { get; private set; }

        public string StreamName { get; private set; }

        public long Version { get; private set; }

        public StreamPointerStatus Status { get; private set; }

        public void Complete() => Status = StreamPointerStatus.Completed;

        public void Cancel() => Status = StreamPointerStatus.Canceled;
    }

    public enum StreamPointerStatus
    {
        Pending = 10,
        Canceled = 50,
        Completed = 100
    }
}