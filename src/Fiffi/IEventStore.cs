using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi
{
	public interface IEventStore : IEventStore<IEvent>
	{ }

	public interface IEventStore<T>
	{
		Task<long> AppendToStreamAsync(string streamName, long version, params T[] events);

		Task<(IEnumerable<T> Events, long Version)> LoadEventStreamAsync(string streamName, long version);
	}
}
