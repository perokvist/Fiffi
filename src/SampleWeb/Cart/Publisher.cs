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
	public class Publisher : BackgroundService
	{
		readonly IReliableStateManager stateManager;

		public Publisher(IReliableStateManager stateManager)
		{
			this.stateManager = stateManager;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await stateManager.DequeueAsync<IEvent>(e => {

					var b = e;
					return Task.CompletedTask;
				}, Serialization.ObjectDeserialization() ,stoppingToken); //TODO json deserializetion
			}
		}
	}
}
