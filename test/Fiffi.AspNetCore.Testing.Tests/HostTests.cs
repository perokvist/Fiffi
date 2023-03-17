using Fiffi.Testing;
using Fiffi.Visualization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit.Abstractions;

namespace Fiffi.AspNetCore.Testing.Tests;

public class HostTests
{
    ITestOutputHelper helper;
    IHost host;

    public HostTests(ITestOutputHelper outputHelper)
    {
        helper = outputHelper;

        host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(webBuilder =>
                webBuilder.ConfigureServices((ctx, services) =>
                services
                .AddFiffiInMemory()
                .AddFiffiModule(TestModule.Initialize)
                .AddInMemoryEventSubscribers())
                .Configure(app => app.UseWelcomePage())
               .UseTestServer())
               .Build();
    }

    [Fact]
    public async Task ResolveModuleAsync()
    {
        await host.StartAsync();
        host.GetTestServer().Services.GetRequiredService<TestModule>();
    }

    [Fact]
    public async Task EventSubscribersAsync()
    {
        await host.StartAsync();
        var m = host.GetTestServer().Services.GetRequiredService<TestModule>();
        await m.DispatchAsync(new TestCommand(new AggregateId("test")));
        var r = await m.QueryAsync(new TestModule.TestQuery());
        Assert.Equal(1, r.EventCount);
    }

}