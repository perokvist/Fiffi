using System;

namespace Fiffi
{
	public struct AggregateId : IAggregateId
	{
		public AggregateId(string aggregateId)
		{
			Id = aggregateId;
		}

		public AggregateId(Guid aggregateId)
		{
			Id = aggregateId.ToString();
		}

		public int CompareTo(IAggregateId other) => string.Compare(Id, other.Id, StringComparison.Ordinal);

		public string Id { get; }

		public override string ToString() => Id;

		public int CompareTo(object obj) {
			if (obj == null)
				return 1;

			if (obj is IAggregateId)
				return CompareTo((IAggregateId)obj);

			throw new InvalidOperationException("Can't compare with non IAggregateId");
		}
	}
	public interface IAggregateId : IComparable<IAggregateId>, IComparable
	{
		string Id { get; }
	}
}
