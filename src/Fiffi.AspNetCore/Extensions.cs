using Fiffi.Modularization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Fiffi.AspNetCore;
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModule<T>(this IServiceCollection services,
    Func<IAdvancedEventStore, ISnapshotStore, Func<IEvent[], Task>, T> f)
    where T : Module
        => AddModule<T>(services, sp => f);

    public static IServiceCollection AddModule<T>(this IServiceCollection services,
        Func<IServiceProvider, Func<IAdvancedEventStore, ISnapshotStore, Func<IEvent[], Task>, T>> f)
        where T : Module
        => services
        .AddSingleton<T>(sp => f(sp)(
        sp.GetRequiredService<IAdvancedEventStore>(),
        sp.GetRequiredService<ISnapshotStore>(),
        sp.GetRequiredService<Func<IEvent[], Task>>()))
        .AddSingleton<Module>(sp => sp.GetRequiredService<T>());

    public static IServiceCollection AddModule<T>(this IServiceCollection services, Func<T> f)
    where T : Module
    => services
        .AddSingleton(sp => f())
        .AddSingleton<Module>(sp => sp.GetRequiredService<T>());

    public static IServiceCollection AddInMemoryEventSubscribers(this IServiceCollection services)
        => AddInMemoryEventSubscribers(services, sp => events => Task.CompletedTask);

    public static IServiceCollection AddInMemoryEventSubscribers(
        this IServiceCollection services,
        Func<IServiceProvider, Func<IEvent[], Task>> additionalPub)
        => services.AddSingleton<Func<IEvent[], Task>>(sp => events =>
            Task.WhenAll(
                sp.GetRequiredService<IEnumerable<Module>>()
                .Select(m => m.WhenAsync(events))
                .Concat(new[] { additionalPub(sp)(events) })
                ));

    public static IServiceCollection Conditional(this IServiceCollection services,
        Func<bool> condition,
        Func<IServiceCollection, IServiceCollection> whenTrue,
        Func<IServiceCollection, IServiceCollection> whenFalse)
        => services
        .When(condition, whenTrue)
        .When(() => !condition(), whenFalse);

    public static IServiceCollection When(this IServiceCollection services,
    Func<bool> condition,
    Func<IServiceCollection, IServiceCollection> register)
        => condition() ? register(services) : services;

    public static IServiceCollection AddFiffiInMemory(this IServiceCollection services)
    => services
       .AddSingleton<IAdvancedEventStore, InMemory.InMemoryEventStore>()
       .AddTransient<ISnapshotStore, InMemory.InMemorySnapshotStore>();

    public static IServiceCollection AddFiffi(this IServiceCollection services, Action<FiffiOptions> configure)
     => services
        .Configure(configure)
        .AddSingleton(sp => sp.GetRequiredService<IOptions<FiffiOptions>>().Value.JsonSerializerOptions)
        .AddSingleton(sp => sp.GetRequiredService<IOptions<FiffiOptions>>().Value.TypeResolver)
        .AddSingleton<IEventStore>(sp => sp.GetRequiredService<IAdvancedEventStore>());


    public static IServiceCollection AddFiffiOptions(this IServiceCollection services,
        JsonSerializerOptions jsonSerializerOptions,
        Dictionary<string, Type> typeResolver
        )
        => services
            .AddSingleton(jsonSerializerOptions)
            .AddSingleton(TypeResolver.FromMap(typeResolver));
}

public static class ConfigurationExtensions
{
    public static Func<bool> DaprEnabled(this IConfiguration configuration, string value = "DAPR_GRPC_PORT")
        => () => !string.IsNullOrEmpty(configuration.GetValue<string>(value));

    public static Func<bool> FiffiDaprEnabled(this IConfiguration configuration, string value = "FIFFI_DAPR")
    => () => !string.IsNullOrEmpty(configuration.GetValue<string>(value));
}

public class FiffiOptions
{
    public Dictionary<string, Type> TypeResolver { get; set; } = new Dictionary<string, Type>();

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions();
}

public record FiffiConfigurator(IServiceCollection Services);

public static class FiffiConfiguratorExtensions
{
    public static void Foo(this FiffiConfigurator configurator)
    { }
}
