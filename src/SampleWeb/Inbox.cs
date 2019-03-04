using Fiffi;
using Fiffi.ServiceFabric;
using Microsoft.ServiceFabric.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleWeb
{
	public  static class Inbox
	{
		public static Func<IReliableStateManager, ITransaction, IEvent, Task>  Publisher(Func<IEvent, EventData> serializer)
		 => (sm, tx, e) =>
			sm.EnqueuAsync(tx, new IEvent[] { e }, serializer, "inbox");


	}
}
