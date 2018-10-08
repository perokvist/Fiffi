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

		readonly IReliableStateManager stateManager;

		public Subscriber(IReliableStateManager stateManager, params Func<IEvent, Task>[] subscribers)
		{
			this.stateManager = stateManager;
			this.subscribers = subscribers;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				//TODO "external" queue
				await stateManager.DequeueAsync<IEvent>(e => Task.WhenAll(subscribers.Select(when => when(e))), stoppingToken);
			}
		}
	}
}
