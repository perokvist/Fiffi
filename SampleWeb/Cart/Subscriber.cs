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
	public class Subscriber : IHostedService
	{
		readonly Func<IEvent, Task>[] whens;

		readonly IReliableStateManager stateManager;

		readonly CancellationTokenSource cancellationTokenSource;

		public Subscriber(IReliableStateManager stateManager,  params Func<IEvent, Task>[] whens)
		{
			this.stateManager = stateManager;
			this.whens = whens;
			this.cancellationTokenSource = new CancellationTokenSource();
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var source = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationTokenSource.Token, cancellationToken);

			while (!cancellationToken.IsCancellationRequested)
			{
				await stateManager.DequeueAsync<IEvent>(e => Task.WhenAll(whens.Select(when => when(e))), source.Token);
			}
		}

		public Task StopAsync(CancellationToken cancellationToken) {
			this.cancellationTokenSource.Cancel();
			return Task.CompletedTask;
		}
	}
}
