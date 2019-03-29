using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class StateManagerExtensions
	{
		public static async Task PublishAllUnPublishedEventsAsync(this IStateManager stateManager, Func<IEvent[], Task> publish)
			=> (await stateManager.GetAllUnPublishedEventsAsync()).Select(x => stateManager.OnPublish(publish));
		public static Func<IEvent[], Task> OnPublish(this IStateManager stateManager, Func<IEvent[], Task> publish)
			=> events =>
				Task.WhenAll(events
				.GroupBy(x => x.SourceId)
				.Select(async x =>
				{
					await publish(x.ToArray());
					var state = stateManager.GetOutBoxAsync(x.Key);
					await stateManager.ClearOutBoxAsync(x.Key);
				}));
	}
}
