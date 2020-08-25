using Fiffi;
using Fiffi.Modularization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sales
{
    public class SalesModule : Module
    {
        public SalesModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
       : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static SalesModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<SalesModule>((c, p, q, s) => new SalesModule(c, p, q, s))
            .Command<PlaceOrder>(
                Commands.GuaranteeCorrelation<PlaceOrder>(),
                cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new OrderPlaced() }, pub))
            .Command<CompleteOrder>(
                Commands.GuaranteeCorrelation<CompleteOrder>(),
                cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new OrderCompleted() }, pub))
            .Policy<Shipping.GoodsShipped>(Policy.On<Shipping.GoodsShipped>(e => new CompleteOrder() ))
            .Create(store);
    }

    public class PlaceOrder : ICommand
    {
        IAggregateId ICommand.AggregateId => new AggregateId("sales.order");
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class CompleteOrder : ICommand
    {
        IAggregateId ICommand.AggregateId => new AggregateId("sales.order");
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class OrderPlaced : IEvent
    {
        public string SourceId => "sales.order";

        public IDictionary<string, string> Meta { get; set; }
    }

    public class OrderCompleted : IEvent
    {
        public string SourceId => "sales.order";

        public IDictionary<string, string> Meta { get; set; }
    }
}
