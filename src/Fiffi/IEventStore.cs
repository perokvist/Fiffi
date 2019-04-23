using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi
{
	public interface IEventStore
	{
		Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events);

		Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version);
	}
}
