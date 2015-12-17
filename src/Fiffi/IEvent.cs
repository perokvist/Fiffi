using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Fiffi
{
	public interface IEvent
	{
		Guid AggregateId { get; }
		Guid CorrelationId { get; set; }
		Guid EventId { get; }
	}
}