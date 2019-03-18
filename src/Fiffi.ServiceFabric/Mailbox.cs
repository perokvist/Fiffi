using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class Mailbox
	{
		public static Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> Reader(this IReliableStateManager stateManager, Func<EventData, IEvent> deserializer, string mailBoxName)
			=> async (f, token) =>
			{
				await stateManager.UseTransactionAsync(tx =>
				stateManager.DequeueAsync(
				tx,
				e => f(stateManager, tx, e),
				deserializer,
				token, queueName: mailBoxName)
				);
			};

		public static Func<ITransaction, IEvent[], Task> WriterWithTransaction(this IReliableStateManager stateManager, Func<IEvent, EventData> serializer, string mailBoxName)
		=> (tx, events) => stateManager.EnqueuAsync(tx, events, serializer, mailBoxName);

		public static Func<IReliableStateManager, ITransaction, IEvent, Task> WriterWithTransaction(Func<IEvent, EventData> serializer, string mailBoxName)
		 => (sm, tx, e) =>
			sm.EnqueuAsync(tx, new IEvent[] { e }, serializer, mailBoxName);

		public static Func<IEvent, CancellationToken, Task> Writer(this IReliableStateManager sm, Func<IEvent, EventData> serializer, string mailBoxName)
		=> (e, ct) => sm.EnqueuAsync(new IEvent[] { e }, serializer, mailBoxName); //TODO ct
	}
}
