using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class ApplicationService
	{
		public static async Task ExecuteAsync<TState>(IEventStore store, ICommand command, Func<TState, IEvent[]> action, Func<IEvent[], Task> pub)
			where TState : class, new()
		{
			var aggregateName = typeof(TState).Name.Replace("State", "Aggregate").ToLower();
			var streamName = $"{aggregateName}-{command.AggregateId}";
			var happend = await store.LoadEventStreamAsync(streamName, 0);
			var state = happend.Item1.Rehydrate<TState>();
			var events = action(state);

			//TODO prettify
			events.ForEach(x => x.Meta["version"] = (happend.Item2 + 1).ToString());
			events.ForEach(x => x.Meta["streamname"] = streamName);
			events.ForEach(x => x.Meta["aggregatename"] = aggregateName);
			events.ForEach(x => x.Meta["eventId"] = Guid.NewGuid().ToString());
			events.ForEach(x => x.Meta[nameof(EventMetaData.CorrelationId)] = Guid.NewGuid().ToString()); //TODO set from command


			//TODO add metadata
			await store.AppendToStreamAsync(streamName, long.Parse(events.Last().Meta["version"]), events);
			await pub(events);
		}
	}
}
