using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class InMemoryEventCommunication : IEventCommunication
{
	List<Func<IEvent, CancellationToken, Task>> subs = new List<Func<IEvent, CancellationToken, Task>>();

	public Task OnShutdownAsync() => Task.CompletedTask;

	public Task PublichAsync(IEvent @event)
	{
		if (subs.Any())
		{
		   Task.WhenAll(subs.Select(f => f(@event, CancellationToken.None)));
		}

		return Task.CompletedTask;
	}

	public Task SubscribeAsync(Func<IEvent, CancellationToken, Task> onEvent)
	{
		subs.Add(onEvent);
		return Task.CompletedTask;
	}
}
