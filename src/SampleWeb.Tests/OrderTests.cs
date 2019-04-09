using Fiffi.ServiceFabric;
using Fiffi.ServiceFabric.Testing;
using Fiffi.Testing;
using SampleWeb.Order;
using ServiceFabric.Mocks;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SampleWeb.Tests
{
	public class OrderTests
	{
		ITestContext context;
		public OrderTests()
		=> this.context = Fiffi.ServiceFabric.Testing.TestContextBuilder.Create(new MockReliableStateManager(), (stateManager, storeFactory, queue) =>
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
