using Fiffi;
using Fiffi.InMemory;
using Fiffi.Testing;
using Fiffi.Visualization;
using Payment;
using Sales;
using Shipping;
using System.Linq;
using System.Threading.Tasks;
using Warehouse;
using Xunit;
using Xunit.Abstractions;

namespace Order.Tests;

public class OrderTests
{
    private readonly ITestOutputHelper testOutput;

    public OrderTests(ITestOutputHelper testOutput)
    {
        this.testOutput = testOutput;
    }

    [Fact]
    [System.Obsolete]
    public async Task FlowAsync()
    {
        var context = TestContextBuilder.Create<InMemoryEventStore, SalesModule>(
            SalesModule.Initialize,
            PaymentModule.Initialize,
            WarehouseModule.Initialize,
            ShippingModule.Initialize
            );

        await context.WhenAsync(new Sales.PlaceOrder());

        context.Then((events, table) =>
        {
            testOutput.WriteLine(table);
            Assert.True(events.AsEnvelopes().Happened<OrderCompleted>());
        });
    }
}
