using Fiffi;
using System;
using System.Threading;
using System.Threading.Tasks;

public class InMemoryEventCommunication : IEventCommunication
{
	Func<IEvent, CancellationToken, Task> pump;

	public Task OnShutdownAsync() => Task.CompletedTask;

	public Task PublichAsync(IEvent @event) 
	{
		if (pump != null)
			pump(@event, CancellationToken.None);

		return Task.CompletedTask;
	}

	public Task SubscribeAsync(Func<IEvent, CancellationToken, Task> onEvent)
	{
		pump = onEvent;
		return Task.CompletedTask;
	}
}
