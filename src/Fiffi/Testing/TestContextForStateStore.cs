using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

namespace Fiffi.Testing
{
    public class TestContextForStateStore : ITestContext
	{
		IEvent[] events = { };
		readonly Queue<IEvent> q;
		readonly Func<ICommand, Task> dispatch;
		readonly Func<IEvent, Task>[] whens;
		readonly Func<Func<IStateStore, Task>, Task> init;

		public TestContextForStateStore(Func<Func<IStateStore, Task>, Task> init, Func<ICommand, Task> dispatch, Queue<IEvent> q, params Func<IEvent, Task>[] whens)
		{
			this.init = init;
			this.dispatch = dispatch;
			this.whens = whens;
			this.q = q;
		}

		public void Given(params IEvent[] events)
		=> this.init(stateStore =>
            Task.WhenAll(events
            .GroupBy(x => x.SourceId)
            .Select(async x => {
                var e = x.First();
                var id = new AggregateId(e.SourceId);

                var type = Type.GetType(e.Meta["test.statetype"]);
                if (type == null) throw new AggregateException($"Couldn't find type by convension for {e.GetAggregateName()}");
                var state = await stateStore.GetAsync(type, id);

                if (state == null)
                {
                    await stateStore.SaveAsync(id, Activator.CreateInstance(type), x.ToArray());
                    state = await stateStore.GetAsync(type, id);
                }

                foreach (var item in x)
                {
                    ((dynamic)state).When(item);
                }
            }))
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
