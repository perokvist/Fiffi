using Fiffi.Modularization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fiffi.Testing
{
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

        public static ITestContext Create<TPersitance, TModule>(Func<TPersitance, Func<IEvent[], Task>, Module> f)
            where TPersitance : class, IEventStore, new()
            where TModule : Module
            => Create<TPersitance>((store, q) => {
                var module = f(store, q.AsPub());
                return new TestContext(a => a(store), module.DispatchAsync, q, e => module.WhenAsync(e));
                });
    }
}
