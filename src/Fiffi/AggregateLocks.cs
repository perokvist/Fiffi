using System.Threading;

namespace Fiffi;

public class AggregateLocks
{
    readonly Dictionary<IAggregateId, (Guid CorrelationId, SemaphoreSlim Semaphore)> locks;
    readonly Action<string> logger;

    public AggregateLocks() : this(s => { })
    { }

    public AggregateLocks(Action<string> logger)
    {
        this.logger = logger;
        this.locks = new Dictionary<IAggregateId, (Guid, SemaphoreSlim)>();
    }

    public async Task UseLockAsync(IAggregateId aggregateId, Guid correlationId, Func<IEvent[], Task> pub, Func<Func<IEvent[], Task>, Task> executeAction, bool waitForLockRelease = true, int timeout = 2000)
    {
        if (!locks.ContainsKey(aggregateId))
            locks.Add(aggregateId, (correlationId, new SemaphoreSlim(1)));

        var @lock = locks[aggregateId];

        if (@lock.Semaphore.CurrentCount == 0 && @lock.CorrelationId == correlationId)
        {
            logger($"Passedthrough lock due to same correlation : {correlationId}");
            return; //let same correlation pass through, cycles
        }

        await @lock.Semaphore.WaitAsync(TimeSpan.FromMilliseconds(timeout));

        locks[aggregateId] = (correlationId, @lock.Semaphore);

        await executeAction(async events =>
        {

            if (!events.Any())
            {
                locks[aggregateId].Semaphore.Release();
                return;
            }

            await pub(events);

            if (waitForLockRelease)
            {
                if (!@lock.Semaphore.AvailableWaitHandle.WaitOne(timeout))
                    throw new TimeoutException();  //TODO timeoutHandler();
            }
        });

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
