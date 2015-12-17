using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fiffi
{
	public interface IEventBus
	{
		void Run(CancellationToken ct, ILogger l);

		void Subscribe(Func<IEvent[], Task> processor);

		void Subscribe<T>(Func<T, Task> f)
			where T : IEvent;

		Task PublishAsync(params IEvent[] events);
	}
}