using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class Extensions
	{
		public static async Task UseTransactionAsync(this IReliableStateManager stateManager, Func<ITransaction, Task> f, bool autoCommit = true)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				await f(tx);
				if (autoCommit) await tx.CommitAsync();
			}
		}

		public static Uri ResolveReverseProxy(this Uri serviceUri) => new Uri($"http://localhost:19081{serviceUri.AbsolutePath}?PartitionKey=1&PartitionKind=Int64Range");

		public static async Task<Uri> ResolveAsync(this Uri serviceUri)
		{
			var partitionKey = new ServicePartitionKey(1);
			var resolver = ServicePartitionResolver.GetDefault();
			var partition = await resolver.ResolveAsync(serviceUri, partitionKey ?? ServicePartitionKey.Singleton, CancellationToken.None);
			var primaryEndpoint = partition.Endpoints.FirstOrDefault(x => x.Role == System.Fabric.ServiceEndpointRole.StatefulPrimary);

			var addresses = JObject.Parse(primaryEndpoint.Address);
			var p = addresses["Endpoints"].First();
			var url = p.First().Value<string>();
			//var baseUrl = new Uri(url).GetLeftPart(System.UriPartial.Authority);
			return new Uri(url);

		}

		public static Action<Action<ApplicationServiceContext, Dispatcher<ICommand, Task>>> WithContext(this Dispatcher<ICommand, Task> dispatcher, ApplicationServiceContext context)
			=> (f) => f(context, dispatcher);

		public static Action<Action<ApplicationServiceContext, Dispatcher<ICommand, Task>>> Register<T>(this Action<Action<ApplicationServiceContext, Dispatcher<ICommand, Task>>> withContext, Func<ApplicationServiceContext, T, Task> registerWithContext)
			where T : ICommand
			=> withContext.Tap(x => x((ctx, dispatcher) => dispatcher.Register<T>(cmd => registerWithContext(ctx, cmd))));

		public static Func<IEvent[], Task> Merge(this EventProcessor eventProcessor, Func<IEvent[], Task> pub) => events => Task.WhenAll(new[] { eventProcessor.PublishAsync, pub }.Select(x => x(events.ToArray())));
	}
}
