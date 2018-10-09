using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Fiffi
{
	public class AggregateLocks
	{
		readonly Dictionary<IAggregateId, (Guid, SemaphoreSlim)> locks;
		readonly Action<string> logger;

		public AggregateLocks() : this(s => { })
		{}

		public AggregateLocks(Action<string> logger)
		{
			this.logger = logger;
			this.locks = new Dictionary<IAggregateId, (Guid, SemaphoreSlim)>();
		}
		
		//Only release once per aggregate
		public void ReleaseIfPresent(params (IAggregateId AggregateId, Guid CorrelationId)[] executionContexts)
		=> executionContexts.GroupBy(x => x.CorrelationId).Select(x => x.First()).ForEach(x => ReleaseIfPresent(x.AggregateId, x.CorrelationId));

		void ReleaseIfPresent(IAggregateId aggregateId, Guid correlationId)
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
