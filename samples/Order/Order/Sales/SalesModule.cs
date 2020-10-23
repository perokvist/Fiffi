using Fiffi;
using Fiffi.Modularization;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Sales
{
    public class SalesModule : Module
    {
        public SalesModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
       : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static SalesModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new Configuration<SalesModule>((c, p, q, s) => new SalesModule(c, p, q, s))
            .Commands(
                Commands.GuaranteeCorrelation<ICommand>(),
                cmd => cmd switch
                    {
                        PlaceOrder => ApplicationService.ExecuteAsync(cmd, () => new[] { new OrderPlaced() }, pub),
                        CompleteOrder => ApplicationService.ExecuteAsync(cmd, () => new[] { new OrderCompleted () }, pub),
                        _ => Task.CompletedTask
                    })
            .Triggers(async (events , d) => {
                foreach (var e in events)
                {
                    var t = e.Event switch
                    {
                        Shipping.GoodsShipped evt => d(new CompleteOrder()),
                        _ => Task.CompletedTask
                    };
                    await t;
                }
            })
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

    public record OrderPlaced : EventRecord;

    public record OrderCompleted : EventRecord;
}
