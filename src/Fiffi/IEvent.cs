using System;
using System.Collections.Generic;
using System.Text;

namespace Fiffi
{
	public interface IEvent
	{
		Guid AggregateId { get; }
		IDictionary<string, string> Meta { get; set; }
	}

}
