using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleWeb.Cart
{
	public class Subscriber : BackgroundService
	{
		readonly Func<IEvent, Task>[] subscribers;
		readonly Func<Func<IEvent, Task>, CancellationToken, Task> fakeQueue;

		public Subscriber(params Func<IEvent, Task>[] subscribers)
		{
			this.subscribers = subscribers;
			this.fakeQueue = null;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			//partiotioning ?
			while (!stoppingToken.IsCancellationRequested)
			{
				await fakeQueue(e => Task.WhenAll(subscribers.Select(when => when(e))), stoppingToken);
			}
		}
	}
}
