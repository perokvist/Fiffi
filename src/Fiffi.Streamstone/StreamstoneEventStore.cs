using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.Streamstone
{
	public class StreamStoneEventStore : IEventStore
	{
		readonly Func<string, Task<Type>> _typeResolver;
		readonly CloudTable _table;

		//public StreamStoneEventStore(CloudTable table) : this(table, s => Task.FromResult(ToType(s)))
		//{ }

		public StreamStoneEventStore(CloudTable table, Func<string, Task<Type>> typeResolver)
		{
			_table = table;
			_typeResolver = typeResolver;
		}

		public Task<long> AppendToStreamAsync(string streamName, long version, IEvent[] events)
		{
			throw new NotImplementedException();
		}

		public Task<(IEnumerable<IEvent> Events, long Version)> LoadEventStreamAsync(string streamName, int version)
		{
			throw new NotImplementedException();
		}




	}
}
