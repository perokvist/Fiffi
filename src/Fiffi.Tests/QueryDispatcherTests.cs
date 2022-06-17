using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Fiffi.Tests;
public class QueryDispatcherTests
{
    [Fact]
    public async Task StreamQueryAsync()
    {
        var subject = new QueryDispatcher();
        subject.Register<TestStreamQuery, TestItem>(
            q => Enumerable.Range(0, 5).Select((x, i) => new TestItem($"Test{i+1}")).ToAsyncEnumerable());

        var r = subject.HandleStreamAsync(new TestStreamQuery());

        Assert.Equal(5, await r.CountAsync());
        Assert.Equal(5, await r.OfType<TestItem>().CountAsync());
        Assert.Equal("Test1", (await ((IAsyncEnumerable<TestItem>)r).FirstAsync()).Message);
    }

    public record TestItem(string Message = "");
    public record TestStreamQuery() : IStreamQuery<TestItem>;
}
