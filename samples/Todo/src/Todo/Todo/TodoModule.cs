using Fiffi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Todo.Todo
{
	public class TodoModule
	{
		public IDictionary<Guid, TodoTask> Store { get; set; } 

		public Func<IEvent, Task> Pub { get; set; }

		public static TodoModule Initialize(IEventBus pub)
		{

			var store = new Dictionary<Guid, TodoTask>();

			return new TodoModule()
			{
				Pub = @event => pub.PublishAsync(@event)
			};





		} 

	}

	public class TaskCreated : IEvent
	{
		public Dictionary<string, object> Meta { get; set; }
		public Dictionary<string, object> Values { get; set; }
		public Guid AggregateId { get; set; }
		public Guid CorrelationId { get; set; }
		public Guid EventId { get; set; }
	}

	public class TodoTask
	{
	}
}