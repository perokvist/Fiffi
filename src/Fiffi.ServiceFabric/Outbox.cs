using Microsoft.ServiceFabric.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class Outbox
	{
		public static Func<Func<IReliableStateManager, ITransaction, IEvent, Task>, CancellationToken, Task> Reader(this IReliableStateManager stateManager, Func<EventData, IEvent> deserializer)
		 => async (f, token) =>
		 {
			 await stateManager.UseTransactionAsync(tx =>
			 stateManager.DequeueAsync(
			 tx,
			 e => f(stateManager, tx, e),
			 deserializer,
			 token)
			 );
		 };

		public static Func<ITransaction, IEvent[], Task> Writer(this IReliableStateManager stateManager, Func<IEvent, EventData> serializer)
		=> (tx, events) => stateManager.EnqueuAsync(tx, events, serializer);


	}
}
