using Fiffi;
using Microsoft.ServiceFabric.Data;
using System;
using ServiceFabric.Mocks;
using Fiffi.ServiceFabric;
using System.Collections.Generic;
using Fiffi.Testing;

namespace SampleWeb.Tests
{
	public class TestContextBuilder //TODO refactor into SF builder
	{
		//TODO is transaction need, tx created for append
		public static TestContext Create(Func<IReliableStateManager, Func<ITransaction, IEventStore>, Queue<IEvent>, TestContext> f)
		{
			var stateManager = new MockReliableStateManager();
			Func<ITransaction, IEventStore> factory = tx => new ReliableEventStore(stateManager, tx, Serialization.FabricSerialization(), Serialization.FabricDeserialization());
			var q = new Queue<IEvent>();
			return f(stateManager, factory, q);
		}
	}
}
