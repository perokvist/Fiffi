using Fiffi.Modularization;

namespace Fiffi.Testing;

public class TestContextBuilder
{
    public static ITestContext Create<TPersitance>(Func<TPersitance, Queue<IEvent>, ITestContext> f)
        where TPersitance : class, new()
        => Create(() => new TPersitance(), f);

    public static ITestContext Create<TPersitance>(Func<TPersitance> creator, Func<TPersitance, Queue<IEvent>, ITestContext> f)
        where TPersitance : class
    {
        var store = creator();
        var q = new Queue<IEvent>();
        return f(store, q);
    }

    public static ITestContext Create<TPersitance, TModule>(
        Func<TPersitance, Func<IEvent[], Task>, TModule> f,
        params Func<TPersitance, Func<IEvent[], Task>, Module>[] additional)
        where TPersitance : class, IEventStore, new()
        where TModule : Module
        => Create(() => new TPersitance(), f, additional);

    public static ITestContext Create<TPersitance, TModule>(
        Func<TPersitance> creator,
        Func<TPersitance, Func<IEvent[], Task>, TModule> f,
        params Func<TPersitance, Func<IEvent[], Task>, Module>[] additional)
        where TPersitance : class, IEventStore
        where TModule : Module
        => Create(creator, (store, q) =>
        {
            var pub = q.AsPub();
            var module = f(store, pub);
            var additionalModules = additional.Select(x => x(store, pub));
            var allWhens = new Func<IEvent, Task>[] { e => module.WhenAsync(e) }
            .Concat(additionalModules.Select<Module, Func<IEvent, Task>>(x => y => x.WhenAsync(y)))
            .ToArray();
            return new TestContext(async (events, a) =>
            {
                await a(store);
                await module.OnStart(events);
                await Task.WhenAll(additionalModules.Select(x => x.OnStart(events)));
            }, module.DispatchAsync, q, module.QueryAsync, allWhens);
        });

    public static (ITestContext, TModule) CreateWithModule<TPersitance, TModule>(
       Func<TPersitance> creator,
       Func<TPersitance, Func<IEvent[], Task>, TModule> f,
       params Func<TPersitance, Func<IEvent[], Task>, Module>[] additional)
       where TPersitance : class, IEventStore
       where TModule : Module
    {
        TModule module = null;
        var ctx = Create(creator,
            (store, pub) =>
            {
                module = f(store, pub);
                return module;
            },
        additional);
        return (ctx, module);
    }
}
