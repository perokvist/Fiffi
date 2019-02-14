using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWeb.Tests
{
	public static class Extensions
	{
		public static Func<IEvent[], Task> ToEventLogger(this Queue<IEvent> q)
		=> (events) =>
		{
			events.ForEach(e => q.Enqueue(e));
			return Task.CompletedTask;
		};

		public static bool Happened(this IEnumerable<IEvent> events) => events.Count() > 1;

	}
}
