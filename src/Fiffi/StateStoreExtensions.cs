using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class StateStoreExtensions
	{
		public static async Task PublishAllUnPublishedEventsAsync(this IStateStore stateManager, Func<IEvent[], Task> publish)
			=> await 
			(await stateManager.GetAllUnPublishedEventsAsync())
			.Pipe(async x => await stateManager.OnPublish(publish)(x));

        static Func<IEvent[], Task> OnPublish(this IStateStore stateManager, Func<IEvent[], Task> publish)
			=> events =>
				Task.WhenAll(events
				.GroupBy(x => x.GetKey())
				.Select(async x =>
				{
					await publish(x.ToArray());
					await stateManager.CompleteOutBoxAsync(x.ToArray());
				}));

        static (string SourceId, Guid CorrelationId) GetKey(this IEvent @event) => (@event.SourceId, @event.GetCorrelation());
	}
}
