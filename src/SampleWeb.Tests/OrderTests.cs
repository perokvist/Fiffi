using Fiffi.ServiceFabric;
using Fiffi.Testing;
using SampleWeb.Order;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SampleWeb.Tests
{
	public class OrderTests
	{
		TestContext context;
		public OrderTests()
		=> this.context = TestContextBuilder.Create((stateManager, storeFactory, queue) =>
		{
			var module = OrderModule.Initialize(stateManager, storeFactory, queue.Enqueue, events => Task.CompletedTask);

			return new TestContext(given => stateManager.UseTransactionAsync(tx => given(storeFactory(tx)))
				, module.DispatchAsync, queue, module.WhenAsync);
		});

		[Fact]
		public async Task CreateOrderAsync()
		{
			//Given


			//When
			await context.WhenAsync(new CreateOrderCommand(Guid.NewGuid().ToString()));

			//Then
			context.Then(events => Assert.True(events.OfType<OrderCreatedEvent>().Happened()));
		}
	}
}
