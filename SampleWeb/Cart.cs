using Fiffi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb
{
	public class CartState
	{ }

	public class AddItemCommand : ICommand
	{
		public Guid AggregateId { get; set; } = Guid.NewGuid();
	}

	public class ItemAddedEvent : IEvent
	{
		public Guid AggregateId => throw new NotImplementedException();

		public IDictionary<string, string> Meta { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	}
}
