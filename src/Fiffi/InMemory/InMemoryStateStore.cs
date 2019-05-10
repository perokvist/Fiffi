using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
    public class InMemoryStateStore : NonTransactionalStateStore
    {
        public InMemoryStateStore() : base(new InMemoryEventStore(), new InMemoryStreamOutbox())
        { }
    }
}
