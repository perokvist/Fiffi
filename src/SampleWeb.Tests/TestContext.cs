using Fiffi;
using Microsoft.ServiceFabric.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using ServiceFabric.Mocks;
using Fiffi.ServiceFabric;
using System.Collections.Generic;

namespace SampleWeb.Tests
{
	public class TestContextBuilder //TODO refactor into SF builder
	{
		//TODO is transaction need, tx created for append
		public static TestContext Create(Func<IReliableStateManager, Func<ITransaction, IEventStore>, Queue<IEvent>, TestContext> f)
		{
			var stateManager = new MockReliableStateManager();
			Func<ITransaction, IEventStore> factory = tx => new ReliableEventStore(stateManager, tx, Serialization.FabricSerialization(), Serialization.FabricDeserialization());
			var q = new Queue<IEvent>();
			return f(stateManager, factory, q);
		}
	}

	public class TestContext //TODO breakout
	{
		IEvent[] events = { };
		readonly Queue<IEvent> q;
		readonly Func<ICommand, Task> dispatch;
		readonly Func<IEvent, Task>[] whens;
		readonly Func<Func<IEventStore, Task>, Task> init;

		public TestContext(Func<Func<IEventStore, Task>, Task> init, Func<ICommand, Task> dispatch, Queue<IEvent> q, params Func<IEvent, Task>[] whens)
		{
			this.init = init;
			this.dispatch = dispatch;
			this.whens = whens;
			this.q = q;
		}

		public void Given(params IEvent[] events)
		=> this.init(store =>
			Task.WhenAll(events
			  .GroupBy(x => x.GetStreamName())
			  .Select(x => store.AppendToStreamAsync(x.Key, 0, x.ToArray())))
		   ).GetAwaiter().GetResult();

		public Task WhenAsync(IEvent @event)
			=> Task.WhenAll(this.whens.Select(w => w(@event)));

		public Task WhenAsync(ICommand command)
		 => WhenAsync(() => this.dispatch(command));

		public async Task WhenAsync(Func<Task> f)
		{
			await f();
			while (this.q.Any())
			{
				var e = this.q.Dequeue();
				await Task.WhenAll(this.whens.Select(w => w(e)));
				this.events = this.events.Concat(new IEvent[] { e }).ToArray();
			}
		}

		public void Then(Action<IEvent[]> f) => f(this.events);
	}
}
