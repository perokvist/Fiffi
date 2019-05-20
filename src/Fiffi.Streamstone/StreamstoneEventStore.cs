using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.Streamstone
{
	public class StreamStoneEventStore : IEventStore
	{
		readonly Func<string, Type> _typeResolver;
		readonly CloudTable _table;

		public StreamStoneEventStore(CloudTable table, Func<string, Type> typeResolver)
		{
			_table = table;
			_typeResolver = typeResolver;
		}

		public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events)
			=> _table.AppendToEventStreamAsync(streamName, version, events);

		public Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, long version)
			=> _table.ReadEventsFromStreamAsync(streamName, version, _typeResolver);



	}
}
