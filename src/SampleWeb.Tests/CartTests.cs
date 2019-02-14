using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SampleWeb.Order;
using Fiffi.ServiceFabric;

namespace SampleWeb.Tests
{
	public class CartTests
	{
		TestContext context;
		public CartTests()
		=> this.context = TestContextBuilder.Create((stateManager, storeFactory, queue) =>
		   {
			   var orderModule = OrderModule.Initialize(stateManager, storeFactory, queue.ToEventLogger());
			   var module = CartModule.Initialize(stateManager, storeFactory, queue.ToEventLogger());

			   return new TestContext(given => stateManager.UseTransactionAsync(tx => given(storeFactory(tx)))
				   , module.DispatchAsync, queue, module.WhenAsync, orderModule.WhenAsync);
		   });

		[Fact]
		public async Task AddItemToCartAsync()
		{
			//Given


			//When
			await context.WhenAsync(new AddItemCommand(Guid.NewGuid()));

			//Then
			context.Then(events => Assert.True(events.OfType<ItemAddedEvent>().Happened()));
		}

		[Fact]
		public async Task CheckoutCartAsync()
		{
			//Given


			//When
			await context.WhenAsync(new CheckoutCommand(Guid.NewGuid()));

			//Then
			context.Then(events => Assert.True(events.OfType<OrderCreatedEvent>().Happened()));
		}

	}
}
