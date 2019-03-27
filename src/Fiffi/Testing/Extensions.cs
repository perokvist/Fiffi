using Fiffi.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi.Testing
{
	public static class Extensions
	{
		public static bool Happened(this IEnumerable<IEvent> events) => events.Count() >= 1;

	}
}
