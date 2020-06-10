using Fiffi;
using Fiffi.Modularization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sales
{
    public class SalesModule : Module
    {
        public SalesModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher)
       : base(dispatcher, publish, queryDispatcher)
        { }

        public static SalesModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<SalesModule>((c, p, q) => new SalesModule(c, p, q))
            .Command<Order>(
                Commands.GuaranteeCorrelation<Order>(),
                cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new OrderPlaced() }, pub))
            .Create(store);
    }

    public class Order : ICommand
    {
        IAggregateId ICommand.AggregateId => new AggregateId("order");
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class OrderPlaced : IEvent
    {
        public string SourceId => "order";

        public IDictionary<string, string> Meta { get; set; }
    }
}
