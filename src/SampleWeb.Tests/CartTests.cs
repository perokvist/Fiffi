using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using SampleWeb.Order;
using Fiffi.ServiceFabric;
using Xunit.Abstractions;

namespace SampleWeb.Tests
{
	public class CartTests
	{
		TestContext context;
		ITestOutputHelper output;
		public CartTests(ITestOutputHelper output)
		=> this.context = TestContextBuilder.Create((stateManager, storeFactory, queue) =>
		   {
			   this.output = output;
			   var orderModule = OrderModule.Initialize(stateManager, storeFactory, queue.Enqueue, events => Task.CompletedTask);
			   var module = CartModule.Initialize(stateManager, storeFactory, queue.Enqueue, events => Task.CompletedTask); //write to xunit output as logger

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
			context.Then((events, table) =>
			{
				this.output.WriteLine(table);
				Assert.True(events.OfType<OrderCreatedEvent>().Happened());
			});
		}

	}
}
