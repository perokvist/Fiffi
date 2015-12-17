using Fiffi;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Todo.Todo
{
	public class TodoModule
	{
		public Func<IEvent, Task> PublishAsync { get; private set; }
		public static Func<Guid> IdGenerator { get; set; } 

		public static TodoModule Initialize(IEventBus pub)
		{
			var store = new Dictionary<Guid, TodoTask>();

			return new TodoModule()
			{
				PublishAsync = @event => pub.PublishAsync(@event)
			};
		} 

	}

	public class TaskCreated : IEvent
	{
		public Guid AggregateId { get; set; }
		public Guid CorrelationId { get; set; }
		public Guid EventId { get; } = Guid.NewGuid();

		public string Name { get; set; }
	}

	public class TodoTask
	{
	}
}