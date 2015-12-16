using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fiffi
{
	public interface IEvent
	{
		IEvent Create(IImmutableDictionary<string, object> meta, IImmutableDictionary<string, object> values);

		IImmutableDictionary<string, object> Meta { get;}
		IImmutableDictionary<string, object> Values { get; }

		Guid AggregateId { get; }
		Guid CorrelationId { get; }
		Guid EventId { get; }
	}
}