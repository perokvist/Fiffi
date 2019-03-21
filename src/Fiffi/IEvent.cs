using System.Collections.Generic;

namespace Fiffi
{
	public interface IEvent
	{
		string SourceId { get; }
		IDictionary<string, string> Meta { get; set; }
	}

}
