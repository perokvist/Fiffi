using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fiffi
{
	public interface IEventBus
	{
		void Run(CancellationToken ct, ILogger l);

		void Register(Func<IEvent[], Task> processor);

		void Register<T>(Func<T, Task> f)
			where T : IEvent;

		Task PublishAsync(params IEvent[] events);
	}
}