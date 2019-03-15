using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class Subscriber : IHostedService
	{
		readonly Func<IEvent, CancellationToken,  Task> inboxPublisher; //TODO alias
		readonly Func<Func<IEvent, CancellationToken, Task>, Task> inboundSubscriber;
		readonly Func<Task> inboundShutDown;

		public Subscriber(
			Func<IEvent, CancellationToken, Task> inboxPublisher,
			Func<Func<IEvent, CancellationToken, Task>, Task> inboundSubscriber,
			Func<Task> inboundShutDown)
		{
			this.inboundShutDown = inboundShutDown;
			this.inboundSubscriber = inboundSubscriber;
			this.inboxPublisher = inboxPublisher;
		}

		public Task StartAsync(CancellationToken cancellationToken)
			=> inboundSubscriber((e, ct) => inboxPublisher(e, ct)); //TODO join CTS?

		public Task StopAsync(CancellationToken cancellationToken)
			=> inboundShutDown();
	}
}
