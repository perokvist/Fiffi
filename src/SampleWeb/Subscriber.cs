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
	public class Subscriber : BackgroundService
	{
		readonly Func<IReliableStateManager, ITransaction, IEvent, Task> inboxPublisher;
		readonly Func<Func<IEvent, Task>, Task> inboundSubscriber;

		public Subscriber(
			Func<IReliableStateManager, ITransaction, IEvent, Task> inboxPublisher,
			Func<Func<IEvent, Task>, Task> inboundSubscriber)
		{
			this.inboundSubscriber = inboundSubscriber;
			this.inboxPublisher = inboxPublisher;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var wait = true;
			while (!stoppingToken.IsCancellationRequested)
			{

				if (wait) await Task.Delay(500);

				wait = true;
			}
		}
	}
}
