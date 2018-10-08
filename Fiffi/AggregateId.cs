using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi
{
	public class AggregateId : IAggregateId
	{
		public AggregateId(string aggregateId)
		{
			Id = aggregateId;
		}

		public string Id { get; }
	}
	public interface IAggregateId
	{
		string Id { get; }
	}
}
