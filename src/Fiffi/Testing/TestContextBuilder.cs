using System;
using System.Collections.Generic;

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
    }
}
