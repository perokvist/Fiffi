using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class StateManagerExtensions
	{
		public static async Task PublishAllUnPublishedEventsAsync(this IStateManager stateManager, Func<IEvent[], Task> publish)
			=> (await stateManager.GetAllUnPublishedEvents()).Select(x => stateManager.OnPublish(publish)); 
	}
}
