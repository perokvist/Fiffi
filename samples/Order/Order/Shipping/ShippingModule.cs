using Fiffi;
using Fiffi.Modularization;
using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shipping
{
    public class ShippingModule : Module
    {
        public ShippingModule(Dispatcher<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher)
       : base(dispatcher, publish, queryDispatcher)
        { }

        public static ShippingModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
            => new ModuleConfiguration<ShippingModule>((c, p, q) => new ShippingModule(c, p, q))
              .Command<Ship>(
                Commands.GuaranteeCorrelation<Ship>(),
                cmd => ApplicationService.ExecuteAsync(cmd, () => new[] { new GoodsShipped() }, pub))
            .Projection<Payment.PaymentRecieved>(e => store.AppendToStreamAsync("order", e))
            .Projection<Warehouse.GoodsPicked>(e => store.AppendToStreamAsync("order", e))
            .Policy<Payment.PaymentRecieved>((e, ctx) => ctx.ExecuteAsync<Order>("order", p => Policy.Issue(e, () => ShippingPolicy.When(e, p))))
            .Policy<Warehouse.GoodsPicked>((e, ctx) => ctx.ExecuteAsync<Order>("order", p => Policy.Issue(e, () => ShippingPolicy.When(e, p))))
            .Create(store);
    }

    public class Order
    {
        public bool Payed;
        public bool Packed;

        public Order When(IEvent @event) => this;

        public Order When(Payment.PaymentRecieved @event) => this.Tap(x => x.Payed = true);
        public Order When(Warehouse.GoodsPicked @event) => this.Tap(x => x.Packed = true);

    }

    public class ShippingPolicy
    {
        public static ICommand When(Payment.PaymentRecieved @event, Order state) => Ship(state);
        public static ICommand When(Warehouse.GoodsPicked @event, Order state) => Ship(state);

        private static ICommand Ship(Order state)
        {
            if (state.Packed && state.Payed)
                return new Ship();

            return null;
        }
    }

    public class Ship : ICommand
    {
        public IAggregateId AggregateId => new AggregateId("shipping.order");

        public Guid CorrelationId { get; set; }
        public Guid CausationId { get; set; }
    }

    public class GoodsShipped : IEvent
    {
        public string SourceId => "shipping.order";

        public IDictionary<string, string> Meta { get; set; }
    }
}
