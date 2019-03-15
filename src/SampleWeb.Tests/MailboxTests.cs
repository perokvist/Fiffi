using Fiffi;
using Fiffi.ServiceFabric;
using Fiffi.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace SampleWeb.Tests
{
	public class MailboxTests
	{
		private IReliableStateManager stateManager;
		private HttpClient client;
		private TestContext context;
		private List<IEvent> events = new List<IEvent>();
		private List<IEvent> outgoingEvents = new List<IEvent>();
		private Task eventsDispatchedToModules;
		private Task eventsDispatchedToOutbound;

		private IEventCommunication eventCommunication;

		public MailboxTests()
		=> this.context = TestContextBuilder.Create((stateManager, storeFactory, queue) =>
		{
			var eventsDispatchedToOutBoundSource = new TaskCompletionSource<bool>();
			eventsDispatchedToOutbound = eventsDispatchedToOutBoundSource.Task;

			eventCommunication = new InMemoryEventCommunication();
			eventCommunication.SubscribeAsync((e, ct) =>
			{
				outgoingEvents.Add(e);
				eventsDispatchedToOutBoundSource.SetResult(true);
				return Task.CompletedTask;
			}).Wait();

		var eventsDispatchedToModulesSource = new TaskCompletionSource<bool>();
		eventsDispatchedToModules = eventsDispatchedToModulesSource.Task;

			var server = new TestServer(
		   new WebHostBuilder()
		   .UseEnvironment("Development")
		   .UseStartup<Startup>()
		   .ConfigureTestServices(services =>
		   {
			   services.AddSingleton(stateManager);
			   services.Configure<MailboxOptions>(opt =>
			   {
				   opt.Serializer = Serialization.FabricSerialization(); //TODO JSON
				   opt.Deserializer = Serialization.FabricDeserialization();
			   });
			   services.AddMailboxes(eventCommunication, sp => new Func<IEvent, Task>[] { e => {
				   events.Add(e);
					eventsDispatchedToModulesSource.SetResult(true);
				   return eventsDispatchedToModulesSource.Task;
			   } });
		   }));

			this.stateManager = stateManager;

			this.client = server.CreateClient();

			return new TestContext(given => stateManager.UseTransactionAsync(tx => given(storeFactory(tx))),
				c => Task.CompletedTask, queue, e => Task.CompletedTask);
		});

		[Fact]
		public async Task InboxProcessorReadsFromInboxAsync()
		{
			await stateManager.EnqueuAsync(new TestEvent(Guid.NewGuid()), Serialization.FabricSerialization(), "inbox");
			await eventsDispatchedToModules; //TODO utlize
			Assert.True(this.events.Any());
		}

		[Fact]
		public async Task SubscriberForwardsToInboxAsync()
		{
			await this.eventCommunication.PublichAsync(new TestEvent(Guid.NewGuid()));
			await eventsDispatchedToModules; //TODO utlize
			Assert.True(this.events.Any());
		}

		[Fact]
		public async Task PublisherForwardsToOutboundAsync()
		{
			await stateManager.EnqueuAsync(new TestEvent(Guid.NewGuid()), Serialization.FabricSerialization(), "outbox");
			await eventsDispatchedToOutbound; //TODO utlize
			Assert.True(this.outgoingEvents.Any());
		}

		public class TestEvent : IEvent
		{
			public TestEvent(Guid id)
			{
				this.AggregateId = id;
				this.Meta["eventid"] = Guid.NewGuid().ToString();
			}

			public Guid AggregateId { get; set; }

			public IDictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
		}
	}
}
