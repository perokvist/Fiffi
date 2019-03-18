using Fiffi;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SampleWeb
{
	using InboxPublisher = Func<IReliableStateManager, ITransaction, IEvent, Task>;

	public class Publisher : BackgroundService
	{
		readonly InboxPublisher inboxPublisher;
		readonly Func<IEvent, Task> outboundPublisher;
		readonly Func<InboxPublisher, CancellationToken, Task> outboxReader;
		readonly Func<Task> outboundShutDown;

		public Publisher(
			Func<InboxPublisher, CancellationToken, Task> outboxReader,
			InboxPublisher inboxPublisher,
			Func<IEvent, Task> outboundPublisher,
			Func<Task> outboundShutDown)
		{
			this.outboundShutDown = outboundShutDown;
			this.outboxReader = outboxReader;
			this.inboxPublisher = inboxPublisher;
			this.outboundPublisher = outboundPublisher;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			await Polling.PollAsync(stoppingToken, f => outboxReader((sm, tx, e) => Task.WhenAll(f(), inboxPublisher(sm, tx, e), outboundPublisher(e)), stoppingToken));
			await outboundShutDown();
		}
	}
}
