using Fiffi;
using System.Linq;

namespace TTD.Domain
{
    public static class StreamExtensions
    {
        public static IEvent[] Append(this IEvent[] current, IEvent[] events)
            => current.Concat(events).ToArray();
    }
}
