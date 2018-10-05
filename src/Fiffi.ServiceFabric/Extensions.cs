using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi.ServiceFabric
{
	public static class Extensions
	{
		public static async Task UseTransactionAsync(this IReliableStateManager stateManager, Func<ITransaction, Task> f, bool autoCommit = true)
		{
			using (var tx = stateManager.CreateTransaction())
			{
				await f(tx);
				if (autoCommit) await tx.CommitAsync();
			}
		}

	}
}
