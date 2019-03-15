using Microsoft.ServiceFabric.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class Inbox
	{

		public static Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> Reader(this IReliableStateManager stateManager, Func<EventData, IEvent> deserializer)
		=> async (f, token) =>
		{
			await stateManager.UseTransactionAsync(tx =>
			stateManager.DequeueAsync(
			tx,
			e => f(stateManager, tx, e),
			deserializer,
			token, queueName: "inbox")
			);
		}; //TODO mailboxes similar use only mailbox?

		public static Func<IReliableStateManager, ITransaction, IEvent, Task> Writer(Func<IEvent, EventData> serializer)
		 => (sm, tx, e) =>
			sm.EnqueuAsync(tx, new IEvent[] { e }, serializer, "inbox");

		public static Func<IEvent, CancellationToken, Task> Writer(this IReliableStateManager sm, Func<IEvent, EventData> serializer)
		=> (e, ct) => sm.EnqueuAsync(new IEvent[] { e }, serializer, "inbox"); //TODO ct

	}
}
