using System;
using System.Collections.Generic;
using System.Linq;

namespace Fiffi.MessageVault
{
	public static class Events
	{
		public static Dictionary<string, Type> GetEventTypes()
			=> AppDomain
				.CurrentDomain.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(x => x.GetInterfaces().Any(i => i == typeof (IEvent)))
				.ToDictionary(type => type.Name, type => type);
	}
}