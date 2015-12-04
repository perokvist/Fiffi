using System;
using System.Collections.Generic;

namespace Fiffi
{
	public interface IEvent
	{
		Dictionary<string, object> Meta { get; set; }
		Dictionary<string, object> Values { get; set; }

		Guid AggregateId { get; set; }
		Guid CorrelationId { get; set; }
		Guid EventId { get; set; }
	}
}