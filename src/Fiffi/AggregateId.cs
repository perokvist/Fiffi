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

		public int CompareTo(IAggregateId other) => string.Compare(Id, other.Id, StringComparison.Ordinal);

		public string Id { get; }

		public override string ToString() => Id;
	}
	public interface IAggregateId
	{
		string Id { get; }
	}
}
