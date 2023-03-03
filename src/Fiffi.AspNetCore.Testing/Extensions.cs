using Fiffi.Modularization;
using Fiffi.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fiffi.AspNetCore.Testing;

public record TestRegistration(ITestContext Context, Module Module);

public static class Extensions
{
    public static (IHost, ITestContext) CreateTestContextAndHostFromServices<TModule>(
    this IHostBuilder hostBuilder,
    Action<IServiceCollection> testServices,
    Func<IServiceProvider, IAdvancedEventStore, Func<IEvent[], Task>, TModule> f
    ) where TModule : Module
        => CreateTestContextAndHostFromServices(hostBuilder, testServices, f, sp => e => Task.CompletedTask);

    /// <summary>
    /// Create TestContext for integration tests
    /// </summary>
    /// <typeparam name="TModule"></typeparam>
    /// <param name="hostBuilder"></param>
    /// <param name="testServices"></param>
    /// <param name="f"></param>
    /// <param name="integrationPublisher"></param>
    /// <remarks>Text context creation doesn't use registered event subscribers. The subject module subscribes trough the test spy. 
    /// Register additional publishers in test using integrationPublisher</remarks>
    /// <returns>Tuple of host and context</returns>
    public static (IHost, ITestContext) CreateTestContextAndHostFromServices<TModule>(
        this IHostBuilder hostBuilder,
        Action<IServiceCollection> testServices,
        Func<IServiceProvider, IAdvancedEventStore, Func<IEvent[], Task>, TModule> f,
        Func<IServiceProvider, Func<IEvent[], Task>> integrationPublisher
        ) where TModule : Module
    {
        var host = hostBuilder
        .ConfigureWebHost(webHost => webHost
        .ConfigureTestServices(services =>
            services
                .Tap(testServices)
                .AddSingleton<TestRegistration>(sp =>
                {
                    TModule m = null;
                    var tc = TestContextBuilder.Create(
                        () => sp.GetRequiredService<IAdvancedEventStore>(),
                        (store, pub) =>
                        {
                            var integrationPub = integrationPublisher(sp);
                            m = f(sp, store, e => Task.WhenAll(pub(e), integrationPub(e)));
                            return m;
                        });
                    return new(tc, m);
                })
                .AddSingleton(sp => (TModule)sp.GetRequiredService<TestRegistration>().Module)
        )
        .UseTestServer())
        .Build();
        return (host, host.Services.GetRequiredService<TestRegistration>().Context);
    }
}
