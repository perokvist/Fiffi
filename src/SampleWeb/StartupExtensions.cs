using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Data;
using System;
using System.Threading.Tasks;

namespace SampleWeb
{
	public static class StartupExtensions
	{

		public static IServiceCollection AddMailboxes(this IServiceCollection services, Func<IServiceProvider, Func<IEvent, Task>[]> subscribers)
		{
			var deserializer = Serialization.JsonDeserialization(TypeResolver.Default());
			var serializer = Serialization.Json();

			//TODO add options for serialization

			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sc => new Publisher(Outbox.Reader(sc.GetRequiredService<IReliableStateManager>(), deserializer), Inbox.Writer(serializer), e => Task.CompletedTask));
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sc => new Subscriber(Inbox.Writer(serializer), f => Task.CompletedTask));
			services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService>(sc => new InboxProcessor(Inbox.Reader(sc.GetRequiredService<IReliableStateManager>(), deserializer), subscribers(sc)));

			return services;
		}

	}
}
