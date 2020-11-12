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
        public WarehouseModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
       : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static WarehouseModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new Configuration<WarehouseModule>((c, p, q, s) => new WarehouseModule(c, p, q, s))
            .Commands(
                Commands.GuaranteeCorrelation<ICommand>(),
                cmd => cmd switch
                { 
                   PickGoods => ApplicationService.ExecuteAsync(cmd, () => new[] { new GoodsPicked() }, pub),
                    _ => Task.CompletedTask
                })
            .Triggers(async(events, d) => {
                foreach (var e in events)
                {
                    var t = e.Event switch {
                        OrderPlaced evt => d(e, new PickGoods()),
                        _ => Task.CompletedTask
                    };
                    await t;
                }
            })
            .Create(store);
    }

    public class PickGoods : ICommand
    {
        IAggregateId ICommand.AggregateId => new AggregateId("warehouse.order");
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public record GoodsPicked : EventRecord;
        //public string SourceId => "warehouse.order";
}
