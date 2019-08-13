using Microsoft.Azure.Documents.ChangeFeedProcessor.PartitionManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.CosmoStore.Configuration
{
    public static class Extensions
    {
        public static IServiceCollection AddModule<TModule, TOptions>(this
            IServiceCollection sc,
            IConfiguration configuration,
            Func<IServiceProvider, IEventStore, TModule> moduleFactory,
            Action<TOptions> options)
            where TModule : class
            where TOptions : ModuleOptions, new()
            => sc
                .Tap(x =>
                x.AddOptions<TOptions>()
                .Bind(configuration)
                .Configure(options)
                .ValidateDataAnnotations())
            .AddSingleton<IEventStore>(sp => sp.GetRequiredService<IOptions<TOptions>>()
            .Value.Pipe(c => new CosmoStoreEventStore(c.ServiceUri, c.Key, c.TypeResolver)))
            .AddSingleton(sp => moduleFactory(sp, sp.GetRequiredService<IEventStore>()));


        public static IServiceCollection AddChangeFeedSubscription<TModule, TOptions>(
            this IServiceCollection sc,
            Func<IServiceProvider, TModule, ILogger, Func<IEvent[], Task>> dispatcherProvider)
            where TModule : class
            where TOptions : ModuleOptions, new()
            => sc
            .AddSingleton<Func<Task<IChangeFeedProcessor>>>(sp => async () =>
            {
                var opt = sp.GetRequiredService<IOptions<TOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<TModule>>();
                var module = sp.GetRequiredService<TModule>();
                var cf = await ChangeFeed
                .CreateProcessorAsync(opt.ServiceUri, opt.Key, opt.HostName, opt.TypeResolver, dispatcherProvider(sp, module, logger), logger);
                return cf;
            })
            .AddHostedService<ChangeFeedHostedService>();
    }
}
