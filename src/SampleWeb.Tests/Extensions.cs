using Fiffi;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWeb.Tests
{
	public static class Extensions
	{
		public static Task Enqueue(this Queue<IEvent> q, ITransaction tx,  params IEvent[] events)
		{
			events.ForEach(e => q.Enqueue(e));
			return Task.CompletedTask;
		}

		public static bool Happened(this IEnumerable<IEvent> events) => events.Count() >= 1;

	}
}
