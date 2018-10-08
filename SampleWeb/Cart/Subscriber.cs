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
		readonly Func<IEvent, Task>[] whens;

		readonly IReliableStateManager stateManager;

		public Subscriber(IReliableStateManager stateManager, params Func<IEvent, Task>[] whens)
		{
			this.stateManager = stateManager;
			this.whens = whens;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await stateManager.DequeueAsync<IEvent>(e => Task.WhenAll(whens.Select(when => when(e))), stoppingToken);
			}
		}
	}
}
