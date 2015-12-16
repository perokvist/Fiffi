using Fiffi;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
		public IEvent Create(IImmutableDictionary<string, object> meta, IImmutableDictionary<string, object> values)
		{
			throw new NotImplementedException();
		}

		IImmutableDictionary<string, object> IEvent.Meta { get; }
		IImmutableDictionary<string, object> IEvent.Values { get; }
		public IReadOnlyDictionary<string, object> Meta { get; set; }
		public IReadOnlyDictionary<string, object> Values { get; set; }
		public Guid AggregateId { get; set; }
		public Guid CorrelationId { get; set; }
		public Guid EventId { get; set; }
	}

	public class TodoTask
	{
	}
}