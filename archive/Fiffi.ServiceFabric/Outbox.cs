using Microsoft.ServiceFabric.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class Outbox
	{
		public static Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> Reader(this IReliableStateManager stateManager, Func<EventData, IEvent> deserializer)
			=> Mailbox.Reader(stateManager, deserializer, "outbox");

		public static Func<ITransaction, IEvent[], Task> Writer(this IReliableStateManager stateManager, Func<IEvent, EventData> serializer)
			=> stateManager.WriterWithTransaction(serializer, "outbox");


	}
}
