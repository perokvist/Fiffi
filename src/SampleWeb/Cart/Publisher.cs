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
		readonly Func<EventData, IEvent> deserializer;

		public Publisher(IReliableStateManager stateManager, Func<EventData, IEvent> deserializer)
		{
			this.deserializer = deserializer;
			this.stateManager = stateManager;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				await stateManager.DequeueAsync<IEvent>(e =>
				{
					var b = e;
					return Task.CompletedTask;
				}, deserializer, stoppingToken); 
			}
		}
	}
}
