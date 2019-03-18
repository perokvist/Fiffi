using Fiffi;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class InboxProcessor : BackgroundService
	{
		readonly Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> inboxReader;
		readonly Func<IEvent, Task>[] subscribers;

		public InboxProcessor(
			Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> inboxReader,
			params Func<IEvent, Task>[] subscribers)
		{
			this.inboxReader = inboxReader;
			this.subscribers = subscribers;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		 => await Polling.PollAsync(stoppingToken, f => inboxReader((sm, tx, e) => Task.WhenAll(f(), Task.WhenAll(subscribers.Select(x => x(e)))), stoppingToken));

	}
}
