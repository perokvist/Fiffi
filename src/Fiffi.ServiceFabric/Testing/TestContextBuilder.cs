using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using Fiffi.Testing;

namespace Fiffi.ServiceFabric.Testing
{
    public class TestContextBuilder 
	{
		public static ITestContext Create(IReliableStateManager stateManager ,Func<IReliableStateManager, Func<ITransaction, IEventStore>, Queue<IEvent>, TestContext> f)
		{
			Func<ITransaction, IEventStore> factory = tx => new ReliableEventStore(stateManager, tx, Serialization.FabricSerialization(), Serialization.FabricDeserialization());
			var q = new Queue<IEvent>();
			return f(stateManager, factory, q);
		}
	}
}
