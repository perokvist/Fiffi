using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fiffi
{
	public static class TypeResolver
	{
		public static Func<string, Type> FromMap(Dictionary<string, Type> map) => name => map[name];

		public static Func<string, Type> Default() => name => Type.GetType(name, aName => Assembly.GetCallingAssembly(), (a, n, t) => a.GetType(n, t));

		public static Dictionary<string, Type> GetEventsInNamespace<T>()
	=> GetEventsInAssembly<T>()
		.Where(x => x.Value.Namespace == typeof(T).Namespace)
		.ToDictionary(kv => kv.Key, kv => kv.Value);

		public static Dictionary<string, Type> GetEventsInAssembly<T>()
			=> typeof(T).GetTypeInfo().Assembly.GetTypes()
				.Where(x => x.GetInterfaces().Any(i => i == typeof(IEvent)))
				.ToDictionary(type => type.Name, type => type);

		public static Dictionary<string, Type> GetEventsFromList(params IEvent[] events)
			=> GetEventsFromTypes(events.Select(x => x.GetType()).ToArray());

		public static Dictionary<string, Type> GetEventsFromTypes(params Type[] types)
			=> types
				.ToDictionary(x => x.Name, x => x);
	}
}
