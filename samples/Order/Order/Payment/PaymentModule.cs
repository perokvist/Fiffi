﻿using Fiffi;
using Fiffi.Modularization;
using Sales;
using System;
using System.Collections.Generic;
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
            => new ModuleConfiguration<PaymentModule>((c, p, q, s) => new PaymentModule(c, p, q, s))
            .Command<Pay>(
                Commands.GuaranteeCorrelation<Pay>(),
                cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new PaymentRecieved() } , pub))
            .Policy<OrderPlaced>((e, ctx) => ctx.ExecuteAsync(Policy.Issue(e, () => new Pay())))
            .Create(store);
    }

    public class Pay : ICommand
    {
        IAggregateId ICommand.AggregateId => new AggregateId("payment.order");
        Guid ICommand.CorrelationId { get; set; }
        Guid ICommand.CausationId { get; set; }
    }

    public class PaymentRecieved : IEvent
    {
        public string SourceId => "payment.order";
        public IDictionary<string, string> Meta { get; set; }
    }

}
