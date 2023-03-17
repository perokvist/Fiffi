using Fiffi.Testing;
using Fiffi.Visualization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Fiffi.AspNetCore.Testing.Tests;

public class HostContextTests
{

    ITestContext context;
    ITestOutputHelper helper;
    IHost host;

    public HostContextTests(ITestOutputHelper outputHelper)
    {

        helper = outputHelper;

        (host, context) = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
                webBuilder.ConfigureServices(services =>
                services
                .AddFiffiInMemory()
                .AddFiffiModule(TestModule.Initialize)
                .AddInMemoryEventSubscribers()
                )
                .Configure(app => app.UseWelcomePage()))
            .CreateFiffiTestContext(
                testServices => testServices.AddFiffiInMemory(), 
                TestModule.Initialize);
    }


    [Fact]
    public async Task ResolveModuleAsync()
    {
        await host.StartAsync();
        host.GetTestServer().Services.GetRequiredService<TestModule>();
    }

    [Fact]
    public async Task SimpleEventCaptureAsync()
    {
        await host.StartAsync();

        await context.WhenAsync(new TestCommand(new AggregateId("test")));

        context.Then((events, visual) => Assert.True(events.Happened<TestEventRecord>()));
        await context.ThenAsync(new TestModule.TestQuery(), result => Assert.Equal(1, result.EventCount));
    }

    [Fact]
    public async Task HistoryEventCaptureAsync()
    {
        await host.StartAsync();

        context.Given(EventEnvelope //todo extension
            .Create("test", new TestEventRecord("test"))
            .AddTestMetaData("test"));

        await context.WhenAsync(new TestCommand(new AggregateId("test")));

        context.Then((events, visual) => Assert.True(events.Happened<TestEventRecord>()));
        await context.ThenAsync(new TestModule.TestQuery(), result => Assert.Equal(2, result.EventCount));
    }
}