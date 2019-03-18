using Microsoft.ServiceFabric.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class Inbox
	{

		public static Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> Reader(this IReliableStateManager stateManager, Func<EventData, IEvent> deserializer)
			=> Mailbox.Reader(stateManager, deserializer, "inbox");

		public static Func<IReliableStateManager, ITransaction, IEvent, Task> Writer(Func<IEvent, EventData> serializer)
			=> Mailbox.WriterWithTransaction(serializer, "inbox");

		public static Func<IEvent, CancellationToken, Task> Writer(this IReliableStateManager sm, Func<IEvent, EventData> serializer)
			=> Mailbox.Writer(sm, serializer, "inbox");

	}
}
