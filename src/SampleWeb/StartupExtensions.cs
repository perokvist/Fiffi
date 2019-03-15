using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.ServiceFabric.Data;
using System;
using System.Threading.Tasks;

namespace SampleWeb
{
	public static class StartupExtensions
	{
		public static IServiceCollection AddMailboxes(
			this IServiceCollection services,
			IEventCommunication eventCommunication,
			Func<IServiceProvider, Func<IEvent, Task>[]> subscribers)
		{
			services.AddSingleton<IHostedService>(sc =>
				new Publisher(Outbox.Reader(sc.GetRequiredService<IReliableStateManager>(),
					sc.GetRequiredService<IOptions<MailboxOptions>>().Value.Deserializer),
					Inbox.Writer(sc.GetRequiredService<IOptions<MailboxOptions>>().Value.Serializer),
					eventCommunication.PublichAsync,
					eventCommunication.OnShutdownAsync));

			services.AddSingleton<IHostedService>(sc =>
				new Subscriber(Inbox.Writer(sc.GetRequiredService<IReliableStateManager>(),
					sc.GetRequiredService<IOptions<MailboxOptions>>().Value.Serializer),
					eventCommunication.SubscribeAsync,
					eventCommunication.OnShutdownAsync)
			);

			services.AddSingleton<IHostedService>(sc =>
				new InboxProcessor(Inbox.Reader(sc.GetRequiredService<IReliableStateManager>(), sc.GetRequiredService<IOptions<MailboxOptions>>().Value.Deserializer), subscribers(sc)));

			return services;
		}


	}

	public class MailboxOptions
	{
		public Func<IEvent, EventData> Serializer { get; set; }

		public Func<EventData, IEvent> Deserializer { get; set; }

	}
}
