using System;
using System.Threading.Tasks;

namespace Fiffi
{
	public static class EventCommunicationExtensions
	{
		public static Task PublishAsync(this IEventCommunication eventCommunication, IEvent @event, Func<IEvent, bool> publish)
		{
			if (publish(@event))
				return eventCommunication.PublichAsync(@event);

			return Task.CompletedTask;
		}

		public static Task PublishAsync<T>(this IEventCommunication eventCommunication, IEvent @event)
			=> eventCommunication.PublishAsync(@event, e => typeof(T).IsInstanceOfType(e));

	}
}
