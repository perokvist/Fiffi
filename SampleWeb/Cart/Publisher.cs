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
	public class Publisher : IHostedService
	{
		readonly IReliableStateManager stateManager;

		public Publisher(IReliableStateManager stateManager)
		{
			this.stateManager = stateManager;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await stateManager.DequeueAsync<IEvent>(e => Task.CompletedTask, cancellationToken);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	}
}
