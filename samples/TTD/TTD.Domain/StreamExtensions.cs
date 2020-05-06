using System.Linq;

namespace TTD.Domain
{
    public static class StreamExtensions
    {
        public static Event[] Append(this Event[] current, Event[] events)
            => current.Concat(events).ToArray();
    }
}
