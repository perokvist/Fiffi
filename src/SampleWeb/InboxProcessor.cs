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
		{
			var wait = true;
			while (!stoppingToken.IsCancellationRequested)
			{
				await inboxReader((sm, tx, e) => Task.WhenAll(Task.Factory.StartNew(() => wait = false), Task.WhenAll(subscribers.Select(x => x(e)))), stoppingToken);

				if (wait) await Task.Delay(500);

				wait = true;
			}
		}
	}
}
