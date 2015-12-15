using System;
using System.Collections.Generic;

namespace Fiffi
{
	public interface IEvent
	{
		IReadOnlyDictionary<string, object> Meta { get;}
		IReadOnlyDictionary<string, object> Values { get; }

		Guid AggregateId { get; }
		Guid CorrelationId { get; }
		Guid EventId { get; }
	}
}