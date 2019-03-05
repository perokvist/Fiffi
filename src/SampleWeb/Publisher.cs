using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class Publisher : BackgroundService
	{
		readonly Func<IReliableStateManager, ITransaction, IEvent, Task> inboxPublisher;
		readonly Func<IEvent, Task> outboundPublisher;
		readonly Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> outboxReader;

		public Publisher(
			Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> outboxReader,
			Func<IReliableStateManager, ITransaction, IEvent, Task> inboxPublisher,
			Func<IEvent, Task> outboundPublisher)
		{
			this.outboxReader = outboxReader;
			this.inboxPublisher = inboxPublisher;
			this.outboundPublisher = outboundPublisher;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var wait = true;
			while (!stoppingToken.IsCancellationRequested)
			{
				await outboxReader((sm, tx, e) => Task.WhenAll(Task.Factory.StartNew(() => wait = false), inboxPublisher(sm, tx, e), outboundPublisher(e)), stoppingToken);

				if (wait) await Task.Delay(500);

				wait = true;
			}
		}
	}
}
