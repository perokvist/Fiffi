using Fiffi;
using Fiffi.Modularization;
using Sales;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Warehouse
{
    public class WarehouseModule : Module
    {
        public WarehouseModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher)
       : base(dispatcher, publish, queryDispatcher)
        { }

        public static WarehouseModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<WarehouseModule>((c, p, q) => new WarehouseModule(c, p, q))
            .Command<PickGoods>(
                Commands.GuaranteeCorrelation<PickGoods>(),
                cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new GoodsPicked() }, pub))
            .Policy<OrderPlaced>((e, ctx) => ctx.ExecuteAsync(Policy.Issue(e, () => new PickGoods())))
            .Create(store);
    }

    public class PickGoods : ICommand
    {
        IAggregateId ICommand.AggregateId => new AggregateId("warehouse.order");
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class GoodsPicked : IEvent
    {
        public string SourceId => "warehouse.order";

        public IDictionary<string, string> Meta { get; set; }
    }
}
