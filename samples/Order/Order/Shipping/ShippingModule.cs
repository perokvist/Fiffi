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
            .Projection<Payment.PaymentRecieved>(e => store.AppendToStreamAsync("foo", e))
            .Projection<Warehouse.GoodsPicked>(e => store.AppendToStreamAsync("foo", e))
            .Policy<Payment.PaymentRecieved>((e, ctx) => ctx.ExecuteAsync<ShippingState>("foo", p => Policy.Issue(e, () => ShippingPolicy.When(e, p))))
            .Policy<Warehouse.GoodsPicked>((e, ctx) => ctx.ExecuteAsync<ShippingState>("foo", p => Policy.Issue(e, () => ShippingPolicy.When(e, p))))
            .Create(store);
    }

    public class ShippingState
    {
        public bool Payed;
        public bool Packed;

        public ShippingState When(IEvent @event) => this;

        public ShippingState When(Payment.PaymentRecieved @event) => this.Tap(x => x.Payed = true);
        public ShippingState When(Warehouse.GoodsPicked @event) => this.Tap(x => x.Packed = true);

    }

    public class ShippingPolicy
    {
        public static ICommand When(Payment.PaymentRecieved @event, ShippingState state) => Foo(state);
        public static ICommand When(Warehouse.GoodsPicked @event, ShippingState state) => Foo(state);

        private static ICommand Foo(ShippingState state)
        {
            if (state.Packed && state.Payed)
                return new Ship();

            return null;
        }

    }

    public class Ship : ICommand
    {
        public IAggregateId AggregateId => new AggregateId("ship");

        public Guid CorrelationId { get; set; }
        public Guid CausationId { get; set; }
    }

    public class GoodsShipped : IEvent
    {
        public string SourceId => "ship";

        public IDictionary<string, string> Meta { get; set; }
    }
}
