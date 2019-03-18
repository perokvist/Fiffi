using Fiffi;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SampleWeb
{
	using InboxPublisher = Func<IEvent, CancellationToken, Task>;

	public class Subscriber : IHostedService
	{
		readonly InboxPublisher inboxPublisher; 
		readonly Func<InboxPublisher, Task> inboundSubscriber;
		readonly Func<Task> inboundShutDown;

		public Subscriber(
			InboxPublisher inboxPublisher,
			Func<InboxPublisher, Task> inboundSubscriber,
			Func<Task> inboundShutDown)
		{
			this.inboundShutDown = inboundShutDown;
			this.inboundSubscriber = inboundSubscriber;
			this.inboxPublisher = inboxPublisher;
		}

		public Task StartAsync(CancellationToken cancellationToken)
			=> inboundSubscriber((e, ct) => inboxPublisher(e, ct)); 

		public Task StopAsync(CancellationToken cancellationToken)
			=> inboundShutDown();
	}
}
