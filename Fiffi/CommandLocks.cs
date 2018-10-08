using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Fiffi
{
	public class CommandLocks
	{
		readonly Dictionary<IAggregateId, (Guid, SemaphoreSlim)> locks;

		public CommandLocks()
		{
			this.locks = new Dictionary<IAggregateId, (Guid, SemaphoreSlim)>();
		}


		public void ReleaseIfPresent(IAggregateId aggregateId, Guid correlationId, Action<string> logger)
		{
			if (!locks.ContainsKey(aggregateId))
			{
				logger($"Lock for {aggregateId.Id} not found.");
				return;
			}

			if (locks[aggregateId].Item1 != correlationId)
			{
				logger($"Lock for {aggregateId.Id} with correlation {correlationId} not found.");
				return;
			}

			logger($"Lock for {aggregateId.Id} with correlation {correlationId} about to release.");
			locks[aggregateId].Item2.Release();
			logger($"Lock for {aggregateId.Id} with correlation {correlationId} released.");

		}
	}
}
