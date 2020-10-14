using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fiffi
{
	public interface IEvent
	{
		string SourceId { get; }
		IDictionary<string, string> Meta { get; init; }
	}

}
