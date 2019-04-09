using System;
using System.Collections.Generic;

namespace Fiffi.Testing
{
    public class TestContextBuilder
    {
        public static ITestContext Create<TPersitance>(Func<TPersitance, Queue<IEvent>, ITestContext> f)
            where TPersitance : class, new()
        {
            var store = new TPersitance();
            var q = new Queue<IEvent>();
            return f(store, q);
        }
    }
}
