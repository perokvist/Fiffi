using Fiffi;
using Fiffi.Modularization;
using Sales;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Payment
{
    public class PaymentModule : Module
    {
        public PaymentModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
            Func<IEvent[], Task> onStart)
       : base(dispatcher, publish, queryDispatcher, onStart)
        { }

        public static PaymentModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new Configuration<PaymentModule>((c, p, q, s) => new PaymentModule(c, p, q, s))
            .Commands(
                Commands.GuaranteeCorrelation<ICommand>(),
                cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new PaymentRecieved() }, pub))
            .Triggers(async (events, d) =>
            {
                foreach (var e in events)
                {
                    var t = e.Event switch
                    {
                        OrderPlaced => d(e, new Pay()),
                        _ => Task.CompletedTask
                    };
                    await t;
                }
            })
            .Create(store);
    }

    public class Pay : ICommand
    {
        IAggregateId ICommand.AggregateId => new AggregateId("payment.order");
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public record PaymentRecieved : EventRecord;

}
