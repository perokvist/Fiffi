using Microsoft.ServiceFabric.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public class EventPublisher
	{
		readonly Func<ITransaction, IEvent[], Task> outBoxQueue;
		readonly Func<IEvent[], Task>[] inProcess;
		readonly Func<IEvent[], Task> spy;

		public EventPublisher(Func<ITransaction, IEvent[], Task> outBoxQueue, Func<IEvent[], Task> spy, params Func<IEvent[], Task>[] inProcess)
		{
			this.outBoxQueue = outBoxQueue;
			this.spy = spy;
			this.inProcess = inProcess;
		}

		public Task PublishAsync(ITransaction tx, PublishMode mode, params IEvent[] events)
		{
			switch (mode)
			{
				case PublishMode.All:
					return Task.WhenAll(outBoxQueue(tx, events), spy(events), Task.WhenAll(inProcess.Select(x => x(events))));
				case PublishMode.OutBoxQueue:
					return Task.WhenAll(outBoxQueue(tx, events), spy(events));
				case PublishMode.InProcess:
					return Task.WhenAll(Task.WhenAll(inProcess.Select(x => x(events))), spy(events));
				default:
					return Task.WhenAll(outBoxQueue(tx, events), spy(events), Task.WhenAll(inProcess.Select(x => x(events))));
			}
		}
	}

	public enum PublishMode
	{
		All = 0,
		OutBoxQueue = 10,
		InProcess = 20
	}
}
