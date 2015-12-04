using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fiffi
{
	public static class Concurrency
	{
		public static Func<IEvent,Task> Release(
			IDictionary<Guid, SemaphoreSlim> map, 
			Func<IEvent, Task> f,
			ILogger l)
		{
			return async @event =>
			{
				//TODO log scope
				l.LogDebug("About to handle {Event}", @event);
				await f(@event);
				l.LogDebug("Handled {Event}", @event);

				if (map.ContainsKey(@event.AggregateId))
				{
					map[@event.AggregateId].Release();
					l.LogDebug("Released lock for {Event}", @event);
				}
				else
				{
					l.LogDebug("Lock missing for {Event}", @event);
				}

			};
		}
	}
}