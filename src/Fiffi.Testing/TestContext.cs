using Fiffi;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SampleWeb.Tests
{
	public class TestContext
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
