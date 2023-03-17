using Fiffi.Modularization;
using Fiffi.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fiffi.AspNetCore.Testing;

public record TestRegistration<T>(ITestContext Context, T Module) where T : IModule;

public static class Extensions
{
    public static (IHost, ITestContext) CreateFiffiTestContext<TModule>(
    this IHostBuilder hostBuilder,
    Func<IServiceProvider, Func<IAdvancedEventStore, ISnapshotStore, Func<IEvent[], Task>, TModule>> f)
    where TModule : class, IModule
    => CreateFiffiTestContext(hostBuilder, services => { },
        (sp, store, snap, pub) => f(sp)(store, snap, pub));

    public static (IHost, ITestContext) CreateFiffiTestContext<TModule>(
        this IHostBuilder hostBuilder,
        Func<IAdvancedEventStore, ISnapshotStore, Func<IEvent[], Task>, TModule> f)
        where TModule : class, IModule
        => CreateFiffiTestContext(hostBuilder, services => { },
            (sp, store, snap, pub) => f(store, snap, pub));

    public static (IHost, ITestContext) CreateFiffiTestContext<TModule>(
    this IHostBuilder hostBuilder,
    Action<IServiceCollection> testServices,
    Func<IAdvancedEventStore, ISnapshotStore, Func<IEvent[], Task>, TModule> f)
    where TModule : class, IModule
    => CreateFiffiTestContext(hostBuilder, services => { },
        (sp, store, snap, pub) => f(store, snap, pub));

    /// <summary>
    /// Create TestContext for integration tests
    /// </summary>
    /// <typeparam name="TModule"></typeparam>
    /// <param name="hostBuilder"></param>
    /// <param name="testServices"></param>
    /// <param name="f">Intercept module resolving, making it possible to swap module implementation.</param>
    /// <remarks>Text context creation doesn't use registered event subscribers. The subject module subscribes trough the test spy. 
    /// Register additional publishers in test using integrationPublisher</remarks>
    /// <returns>Tuple of host and context</returns>
    public static (IHost, ITestContext) CreateFiffiTestContext<TModule>(
        this IHostBuilder hostBuilder,
        Action<IServiceCollection> testServices,
        Func<IServiceProvider, IAdvancedEventStore, ISnapshotStore, Func<IEvent[], Task>, TModule> f
        ) where TModule : class, IModule
    {
        var host = hostBuilder
        .ConfigureWebHost(webHost => webHost
        .ConfigureTestServices(services =>
            services
                .Tap(testServices)
                .AddSingleton<TestRegistration<TModule>>(sp =>
                {
                    TModule? m = default;
                    var tc = TestContextBuilder.Create(
                        () => sp.GetRequiredService<IAdvancedEventStore>(),
                        (store, pub) => {
                            m = f(sp, store, sp.GetRequiredService<ISnapshotStore>(), pub);
                            return m;
                        });
                    return new(tc, m);
                })
        .AddFiffiModule(sp => sp.GetRequiredService<TestRegistration<TModule>>().Module)
        )
        .UseTestServer())
        .Build();
        return (host, host.Services.GetRequiredService<TestRegistration<TModule>>().Context);
    }
}
