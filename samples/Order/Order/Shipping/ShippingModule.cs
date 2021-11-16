using Fiffi;
using Fiffi.Modularization;
using Fiffi.Projections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shipping;

public class ShippingModule : Module
{
    public ShippingModule(Func<ICommand, Task> dispatcher, Func<IEvent[], Task> publish, QueryDispatcher queryDispatcher,
        Func<IEvent[], Task> onStart)
   : base(dispatcher, publish, queryDispatcher, onStart)
    { }

    public static ShippingModule Initialize(IAdvancedEventStore store, Func<IEvent[], Task> pub)
        => new Configuration<ShippingModule>((c, p, q, s) => new ShippingModule(c, p, q, s))
          .Commands(
            Commands.GuaranteeCorrelation<ICommand>(),
            cmd => ApplicationService.ExecuteAsync(cmd, () => (new[] { new GoodsShipped() }), pub))
          .Updates(events => store.AppendToStreamAsync("order", events.Filter(typeof(Payment.PaymentRecieved), typeof(Warehouse.GoodsPicked))))
          .Triggers(async (events, d) =>
          {
              foreach (var e in events)
              {
                  var t = e.Event switch
                  {
                      Payment.PaymentRecieved evt => d(e, ShippingPolicy.When(evt, await store.GetAsync<Order>("order"))),
                      Warehouse.GoodsPicked evt => d(e, ShippingPolicy.When(evt, await store.GetAsync<Order>("order"))),
                      _ => Task.CompletedTask
                  };
                  await t;
              }
          })
        .Create(store);
}

public class Order
{
    public bool Payed;
    public bool Packed;

    public Order When(EventRecord @event) => @event switch
    {
        Payment.PaymentRecieved e => When(e),
        Warehouse.GoodsPicked e => When(e),
        _ => this
    };

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

public record GoodsShipped : EventRecord;
