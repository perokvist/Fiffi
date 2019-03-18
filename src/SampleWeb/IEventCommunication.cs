using Fiffi;
using System;
using System.Threading;
using System.Threading.Tasks;

public interface IEventCommunication
{
	Task PublichAsync(IEvent @event);

	Task SubscribeAsync(Func<IEvent, CancellationToken, Task> onEvent); //TODO, CancellationToken cancellationToken);

	Task OnShutdownAsync();
}
