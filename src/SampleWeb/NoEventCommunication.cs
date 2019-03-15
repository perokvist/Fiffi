using Fiffi;
using System;
using System.Threading;
using System.Threading.Tasks;

public class NoEventCommunication : IEventCommunication
{
	public Task OnShutdownAsync() => Task.CompletedTask;

	public Task PublichAsync(IEvent @event) => Task.CompletedTask;

	public Task SubscribeAsync(Func<IEvent, CancellationToken, Task> onEvent) => Task.CompletedTask;
}
