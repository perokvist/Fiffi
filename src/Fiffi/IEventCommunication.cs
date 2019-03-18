using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi
{
	public interface IEventCommunication
	{
		Task PublichAsync(IEvent @event);

		Task SubscribeAsync(Func<IEvent, CancellationToken, Task> onEvent); //TODO, CancellationToken cancellationToken);

		Task OnShutdownAsync();
	}
}
