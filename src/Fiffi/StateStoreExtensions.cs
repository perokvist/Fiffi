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
		public static Func<IEvent[], Task> OnPublish(this IStateStore stateManager, Func<IEvent[], Task> publish)
			=> events =>
				Task.WhenAll(events
				.GroupBy(x => x.GetKey())
				.Select(async x =>
				{
					await publish(x.ToArray());
					await stateManager.ClearOutBoxAsync(x.Key.SourceId, x.Key.CorrelationId);
				}));

        static (string SourceId, Guid CorrelationId) GetKey(this IEvent @event) => (@event.SourceId, @event.GetCorrelation());
	}
}
